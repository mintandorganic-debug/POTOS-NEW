using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardCarouselUI
{

    public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private CardCarouselController carousel;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private RectTransform imageRoot;

        [Header("Description")]
        [SerializeField] private CanvasGroup descriptionGroup;

        [Header("Hover")]
        [SerializeField] private Image cardImage;
        [SerializeField] private Sprite categoryIconSprite;
        [SerializeField] private Vector3 categoryIconScale = Vector3.one;
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float imageHoverScale = 0.9f;
        [SerializeField] private float imageYOffset = 25f;
        [SerializeField] private float transitionDuration = 0.2f;
        [Tooltip("Seconds the description stays hidden after hover starts, before it fades in over Transition Duration.")]
        [SerializeField] private float descriptionShowDelay = 0.1f;

        [Header("Hover Shake")]
        [SerializeField] private float shakeSpeed = 30f;
        [SerializeField] private float shakeDuration = 0.25f;
        [SerializeField] private float shakeStrength = 5f;
    
        [Header("Status Icons")]
        [SerializeField] private CanvasGroup statusIconsGroup;
        [SerializeField] private GameObject[] statusIcons;
        [SerializeField, Range(0, 6)] private int statusIconCount;

        [Header("Frame Light Sweep")]
        [SerializeField] private bool useVFXEffect;
        [SerializeField] private Graphic frameGraphic;
        [SerializeField] private string sweepProperty = "_SweepProgress";
        [SerializeField] private float sweepStart = -2f;
        [SerializeField] private float sweepEnd = 2f;
        [SerializeField] private float sweepDuration = 0.5f;

        [Header("VFX Hover")]
        [SerializeField] private GameObject vfxPrefab;
        [SerializeField] private Transform vfxSpawnPoint;

        private GameObject vfxInstance;
        private ParticleSystem[] vfxParticles;

        private Vector3 imageBaseScale;
        private Vector2 imageBasePosition;

        private Material frameMaterial;

        private Coroutine hoverCoroutine;
        private Coroutine sweepCoroutine;
        private Coroutine shakeCoroutine;

        private bool isHovered;
        private bool isPointerInside;
        private bool isInteractable = true;
        private bool isMissingCard;

        private Sprite baseSprite;

        private const float DefenseSpinSpeed = 180f;
        private static readonly Quaternion DefenseTilt = Quaternion.Euler(-28f, 0f, 0f);
        private static Sprite defenseShadowSprite;
        private RectTransform defenseVisual;
        private RectTransform shadowMask;
        private RectTransform receivedShadow;
        private Quaternion baseVisualRotation;
        private float spinAngle;
        private bool isSpinning;

        private void Awake()
        {
            if (cardImage != null) baseSprite = cardImage.sprite;

            if (statusIconsGroup != null)
            {
                statusIconsGroup.alpha = 0f;
                statusIconsGroup.gameObject.SetActive(false);
            }

            UpdateStatusIcons();

            if (vfxPrefab != null)
            {
                Transform parent = vfxSpawnPoint != null
                    ? vfxSpawnPoint
                    : transform;

                vfxInstance = Instantiate(
                    vfxPrefab,
                    parent.position,
                    parent.rotation,
                    parent
                );

                vfxInstance.transform.localPosition = Vector3.zero;
                vfxInstance.transform.localRotation = Quaternion.identity;

                vfxParticles = vfxInstance.GetComponentsInChildren<ParticleSystem>();
                vfxInstance.SetActive(false);
            }

            if (imageRoot != null)
            {
                imageBaseScale = imageRoot.localScale;
                imageBasePosition = imageRoot.anchoredPosition;
            }

            if (descriptionGroup != null)
            {
                descriptionGroup.alpha = 0f;
                descriptionGroup.gameObject.SetActive(false);
            }

            if (frameGraphic != null && frameGraphic.material != null)
            {
                frameMaterial = new Material(frameGraphic.material);
                frameGraphic.material = frameMaterial;

                if (frameMaterial.HasProperty(sweepProperty))
                    frameMaterial.SetFloat(sweepProperty, sweepStart);
            }

            ConfigureRaycastTargets();
        }

        // Hover visuals (raised image, description, status icons, shake) change the
        // card's shape. When they were raycast targets, a pointer near an edge or in the
        // gap between cards entered/exited every frame and every hover effect flickered.
        // Now only one static, invisible hit area on the card root receives pointer events.
        private void ConfigureRaycastTargets()
        {
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.gameObject != gameObject)
                    graphic.raycastTarget = false;
            }

            if (!TryGetComponent(out Graphic hitArea))
            {
                Image image = gameObject.AddComponent<Image>();
                image.color = Color.clear;
                hitArea = image;
            }

            hitArea.raycastTarget = true;
        }

        private float enabledTime;

        private void OnEnable()
        {
            enabledTime = Time.unscaledTime;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;

            if (isMissingCard) StartShake();
        
            if (!isInteractable) return;

            if (Time.unscaledTime - enabledTime >= 0.25f)
            {
                GameAudioManager.Instance?.PlayHover();
            }
            SetHover(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable)
                return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                GameAudioManager.Instance?.PlayAccept();
                Clicked?.Invoke();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            SetHover(false);
        }

        public event Action<bool> HoverChanged;
        public event Action Clicked;

        public void UseIllustration(RectTransform root, Image image)
        {
            imageRoot = root;
            cardImage = image;
            image.raycastTarget = false;
            baseSprite = image.sprite;
            imageBaseScale = root.localScale;
            imageBasePosition = root.anchoredPosition;
        }

        public void Bind(CardCarouselController owner)
        {
            carousel = owner;
        }

        public void EnableDefenseSpin()
        {
            defenseVisual = visualRoot;
            if (defenseVisual == transform)
            {
                // Keep the root hit area still while the card artwork rotates.
                defenseVisual = (RectTransform)new GameObject("Defense Card Visuals", typeof(RectTransform)).transform;
                defenseVisual.SetParent(transform, false);
                defenseVisual.anchorMin = Vector2.zero;
                defenseVisual.anchorMax = Vector2.one;
                defenseVisual.offsetMin = Vector2.zero;
                defenseVisual.offsetMax = Vector2.zero;

                while (transform.GetChild(0) != defenseVisual)
                    transform.GetChild(0).SetParent(defenseVisual, false);
            }

            if (defenseVisual != null)
                baseVisualRotation = defenseVisual.localRotation;
        }

        private void LateUpdate()
        {
            bool shouldSpin = defenseVisual != null && isInteractable && carousel.IsDefenseFlying((RectTransform)transform);
            if (!shouldSpin)
            {
                if (isSpinning)
                {
                    StopDefenseSpin();
                    if (isInteractable && isPointerInside)
                        SetHover(true);
                }

                return;
            }

            if (!isSpinning)
            {
                isSpinning = true;
                SetHover(false);

                if (shakeCoroutine != null)
                {
                    StopCoroutine(shakeCoroutine);
                    shakeCoroutine = null;
                    visualRoot.localRotation = Quaternion.identity;
                }
            }

            spinAngle = Mathf.Repeat(spinAngle + DefenseSpinSpeed * Time.unscaledDeltaTime, 360f);
            defenseVisual.localRotation = baseVisualRotation * DefenseTilt * Quaternion.Euler(0f, 0f, -spinAngle);
            UpdateDefenseShadows(true);
        }

        private void StopDefenseSpin()
        {
            isSpinning = false;
            spinAngle = 0f;
            if (defenseVisual != null)
                defenseVisual.localRotation = baseVisualRotation;
            UpdateDefenseShadows(false);
        }

        private void UpdateDefenseShadows(bool visible)
        {
            if (carousel == null)
                return;

            for (int i = 0; i < carousel.Cards.Count; i++)
            {
                RectTransform card = carousel.Cards[i];
                if (card == null || card == transform || !card.TryGetComponent(out CardHover neighbor))
                    continue;

                if (visible && card.gameObject.activeInHierarchy)
                    neighbor.ShowDefenseShadow(defenseVisual, spinAngle);
                else
                    neighbor.HideDefenseShadow();
            }
        }

        private void ShowDefenseShadow(RectTransform source, float angle)
        {
            RectTransform card = (RectTransform)transform;
            if (shadowMask == null)
            {
                // Only the surface of this card receives the shadow.
                shadowMask = (RectTransform)new GameObject("Card Shadow Mask", typeof(RectTransform), typeof(RectMask2D)).transform;
                shadowMask.SetParent(card, false);
                shadowMask.anchorMin = Vector2.zero;
                shadowMask.anchorMax = Vector2.one;
                shadowMask.offsetMin = Vector2.zero;
                shadowMask.offsetMax = Vector2.zero;

                var image = new GameObject("Dove Shadow", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                receivedShadow = image.rectTransform;
                receivedShadow.SetParent(shadowMask, false);
                receivedShadow.anchorMin = card.pivot;
                receivedShadow.anchorMax = card.pivot;
                image.sprite = GetDefenseShadowSprite();
                image.color = new Color(0f, 0f, 0f, 0.38f);
                image.raycastTarget = false;
            }

            Vector3 scale = source.lossyScale;
            Vector3 receiverScale = card.lossyScale;
            receivedShadow.sizeDelta = new Vector2(
                source.rect.width * Mathf.Abs(scale.x / receiverScale.x),
                source.rect.height * Mathf.Abs(scale.y / receiverScale.y));

            Vector3 sourceCenter = source.TransformPoint(source.rect.center);
            Vector2 center = card.InverseTransformPoint(sourceCenter);
            float height = receivedShadow.sizeDelta.y;
            receivedShadow.anchoredPosition = center + new Vector2(0.09f, -0.27f) * height;
            receivedShadow.localRotation = Quaternion.Euler(0f, 0f, -angle);
            shadowMask.gameObject.SetActive(true);
        }

        private void HideDefenseShadow()
        {
            if (shadowMask != null)
                shadowMask.gameObject.SetActive(false);
        }

        private static Sprite GetDefenseShadowSprite()
        {
            if (defenseShadowSprite != null)
                return defenseShadowSprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs((x + 0.5f) / size - 0.5f) - 0.36f, 0f);
                    float dy = Mathf.Max(Mathf.Abs((y + 0.5f) / size - 0.5f) - 0.36f, 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    byte alpha = (byte)Mathf.RoundToInt(255f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.06f, 0.14f, distance))));
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            defenseShadowSprite = Sprite.Create(
                texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect);
            defenseShadowSprite.hideFlags = HideFlags.HideAndDontSave;
            return defenseShadowSprite;
        }

        public void SetDescription(string text)
        {
            if (descriptionGroup == null)
                return;

            TMP_Text label =
                descriptionGroup.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
                label.text = text;
        }

        public bool IsPointerInside => isPointerInside;


        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;

            if (!isInteractable && isHovered)
            {
                SetHover(false);
            }
            else if (isInteractable &&
                     isPointerInside &&
                     !isHovered &&
                     gameObject.activeInHierarchy)
            {
                // A side card scrolled to the center under a resting pointer.
                SetHover(true);
            }
        }

        public void SetMissingCard(bool missingCard)
        {
            isMissingCard = missingCard;
        }

        public void SetHover(bool state)
        {
            if (state && defenseVisual != null && carousel.HasDefenseFlight)
                return;

            if (isHovered == state)
                return;

            isHovered = state;
            HoverChanged?.Invoke(state);

            if (hoverCoroutine != null)
                StopCoroutine(hoverCoroutine);

            if (state)
            {
                StartShake();
            
                if (useVFXEffect)
                {
                    StartVFX();
                }
                else
                {
                    StartLightSweep();
                }
            }
            else
            {
                if (useVFXEffect) StopVFX();
            }

            hoverCoroutine = StartCoroutine(AnimateHover(state));
        }

        public void Shake()
        {
            if (gameObject.activeInHierarchy)
                StartShake();
        }

        private void StartShake()
        {
            if (visualRoot == null || isSpinning) return;
        
            if(shakeCoroutine != null) StopCoroutine(shakeCoroutine);

            shakeCoroutine = StartCoroutine((AnimateShkae()));
        }

        private IEnumerator AnimateShkae()
        {
            float time = 0f;

            while (time < shakeDuration)
            {
                time += Time.unscaledDeltaTime;

                float fade = 1f - Mathf.Clamp01(time / shakeDuration);

                float angle = Mathf.Sin(time * shakeSpeed) *
                              shakeStrength *
                              fade;
            
                visualRoot.localRotation = Quaternion.Euler(0, 0, angle);
            
                yield return null;
            }
        
            visualRoot.localRotation = Quaternion.identity;
            shakeCoroutine = null;
        }
    
        private void UpdateStatusIcons()
        {
            if (statusIconsGroup == null) return;

            for (int i = 0; i < statusIcons.Length; i++)
            {
                if (statusIcons[i] != null) statusIcons[i].SetActive(i < statusIconCount);
            }
        }

        private void StartVFX()
        {
            if (vfxInstance == null) return;

            vfxInstance.SetActive(true);

            if (vfxParticles == null) return;

            for (int i = 0; i < vfxParticles.Length; i++)
            {
                vfxParticles[i].Clear();
                vfxParticles[i].Play();
            }
        }

        private void StopVFX()
        {
            if (vfxInstance == null) return;
            if (vfxParticles != null)
            {
                for (int i = 0; i < vfxParticles.Length; i++)
                {
                    vfxParticles[i].Stop(
                        true,
                        ParticleSystemStopBehavior.StopEmittingAndClear
                    );
                }
            }

            vfxInstance.SetActive(false);
        }

        private IEnumerator AnimateHover(bool hover)
        {
            if (hover && cardImage != null)
                cardImage.sprite = categoryIconSprite;

            if (hover && descriptionGroup != null)
                descriptionGroup.gameObject.SetActive(true);

            if (hover && statusIconsGroup != null)
            {
                UpdateStatusIcons();
                statusIconsGroup.gameObject.SetActive(true);
            }

            Vector3 startImageScale = imageRoot != null
                ? imageRoot.localScale
                : Vector3.one;

            Vector2 startImagePosition = imageRoot != null
                ? imageRoot.anchoredPosition
                : Vector2.zero;

            float startDescriptionAlpha = descriptionGroup != null
                ? descriptionGroup.alpha
                : 0f;

            float startStatusAlpha = statusIconsGroup != null
                ? statusIconsGroup.alpha
                : 0f;

            Vector3 targetImageScale = hover
                ? categoryIconScale
                : imageBaseScale;

            Vector2 targetImagePosition = hover
                ? imageBasePosition + Vector2.up * imageYOffset
                : imageBasePosition;

            float targetDescriptionAlpha = hover ? 1f : 0f;
            float targetStatusAlpha = hover ? 1f : 0f;

            float startHoverScale = hover ? 1f : hoverScale;
            float targetHoverScale = hover ? hoverScale : 1f;

            float time = 0f;

            // The description stays hidden for descriptionShowDelay, then fades in
            // over transitionDuration. Hiding has no delay.
            float descriptionDelay = hover ? Mathf.Max(0f, descriptionShowDelay) : 0f;
            float totalDuration = transitionDuration + descriptionDelay;

            while (time < totalDuration)
            {
                bool spriteChanged = false;
                time += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(time / transitionDuration);
                t = Mathf.SmoothStep(0f, 1f, t);

                float currentHoverScale = Mathf.Lerp(
                    startHoverScale,
                    targetHoverScale,
                    t
                );

                if (t >= 0.5f &&
                    !spriteChanged &&
                    cardImage != null)
                {
                    cardImage.sprite = hover
                        ? categoryIconSprite
                        : baseSprite;

                    spriteChanged = true;
                }

                if (statusIconsGroup != null)
                {
                    statusIconsGroup.alpha = Mathf.Lerp(
                        startStatusAlpha,
                        targetStatusAlpha,
                        t
                    );
                }

                carousel.SetHoverScale(
                    visualRoot,
                    currentHoverScale
                );

                if (imageRoot != null)
                {
                    imageRoot.localScale = Vector3.Lerp(
                        startImageScale,
                        targetImageScale,
                        t
                    );

                    imageRoot.anchoredPosition = Vector2.Lerp(
                        startImagePosition,
                        targetImagePosition,
                        t
                    );
                }

                if (descriptionGroup != null)
                {
                    float descriptionElapsed = time - descriptionDelay;

                    float descriptionT = transitionDuration > 0f
                        ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(descriptionElapsed / transitionDuration))
                        : (descriptionElapsed >= 0f ? 1f : 0f);

                    descriptionGroup.alpha = Mathf.Lerp(
                        startDescriptionAlpha,
                        targetDescriptionAlpha,
                        descriptionT
                    );
                }

                yield return null;
            }

            if (imageRoot != null)
            {
                imageRoot.localScale = targetImageScale;
                imageRoot.anchoredPosition = targetImagePosition;
            }

            if (descriptionGroup != null)
                descriptionGroup.alpha = targetDescriptionAlpha;
            if (statusIconsGroup != null)
                statusIconsGroup.alpha = targetDescriptionAlpha;

            if (!hover && descriptionGroup != null)
            {
                descriptionGroup.alpha = 0f;
                descriptionGroup.gameObject.SetActive(false);
            }

            if (!hover && statusIconsGroup != null)
            {
                statusIconsGroup.alpha = 0f;
                statusIconsGroup.gameObject.SetActive(false);
            }

            hoverCoroutine = null;
        }

        private void StartLightSweep()
        {
            if (frameMaterial == null)
                return;

            if (!frameMaterial.HasProperty(sweepProperty))
                return;

            if (sweepCoroutine != null)
                StopCoroutine(sweepCoroutine);

            frameMaterial.SetFloat(sweepProperty, sweepStart);

            sweepCoroutine = StartCoroutine(AnimateLightSweep());
        }

        private IEnumerator AnimateLightSweep()
        {
            float time = 0f;

            while (time < sweepDuration)
            {
                time += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(time / sweepDuration);

                float sweep = Mathf.Lerp(
                    sweepStart,
                    sweepEnd,
                    t
                );

                frameMaterial.SetFloat(sweepProperty, sweep);

                yield return null;
            }

            frameMaterial.SetFloat(sweepProperty, sweepEnd);

            sweepCoroutine = null;
        }

        private void OnDisable()
        {
            if (isSpinning)
                StopDefenseSpin();
            HideDefenseShadow();

            if (vfxInstance != null)
                vfxInstance.SetActive(false);

            if (hoverCoroutine != null)
            {
                StopCoroutine(hoverCoroutine);
                hoverCoroutine = null;
            }

            if (sweepCoroutine != null)
            {
                StopCoroutine(sweepCoroutine);
                sweepCoroutine = null;
            }

            if (isHovered)
                HoverChanged?.Invoke(false);

            isHovered = false;
            isPointerInside = false;

            if (cardImage != null &&
                baseSprite != null)
            {
                cardImage.sprite = baseSprite;
            }

            if (imageRoot != null)
            {
                imageRoot.localScale = imageBaseScale;
                imageRoot.anchoredPosition = imageBasePosition;
            }

            if (descriptionGroup != null)
            {
                descriptionGroup.alpha = 0f;
                descriptionGroup.gameObject.SetActive(false);
            }

            if (frameMaterial != null && frameMaterial.HasProperty(sweepProperty))
                frameMaterial.SetFloat(sweepProperty, sweepStart);
        }
    
        public void ResetVisual()
        {
            isHovered = false;

            if (cardImage != null &&
                baseSprite != null)
            {
                cardImage.sprite = baseSprite;
            }

            if (imageRoot != null)
            {
                imageRoot.localScale = imageBaseScale;
                imageRoot.anchoredPosition = imageBasePosition;
            }

            if (descriptionGroup != null)
            {
                descriptionGroup.alpha = 0f;
                descriptionGroup.gameObject.SetActive(false);
            }

            if (statusIconsGroup != null)
            {
                statusIconsGroup.alpha = 0f;
                statusIconsGroup.gameObject.SetActive(false);
            }
        
        }
    
        public void CacheBaseSprite()
        {
            if (cardImage == null)
                return;

            if (cardImage.sprite == null)
                return;

            if (cardImage.sprite == categoryIconSprite)
                return;

            baseSprite = cardImage.sprite;
        }

        private void OnDestroy()
        {
            if (frameMaterial != null)
                Destroy(frameMaterial);
        }
    }
}
