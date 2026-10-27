using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CardCarouselUI
{

    public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
        private bool isInteractable = true;
        private bool isMissingCard;

        private Sprite baseSprite;

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
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isMissingCard) StartShake();
        
            if (!isInteractable) return;

            SetHover(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHover(false);
        }

        public event Action<bool> HoverChanged;

        public void UseIllustration(RectTransform root, Image image)
        {
            imageRoot = root;
            cardImage = image;
            baseSprite = image.sprite;
            imageBaseScale = root.localScale;
            imageBasePosition = root.anchoredPosition;
        }

        public void Bind(CardCarouselController owner)
        {
            carousel = owner;
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

        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;

            if (!isInteractable && !isHovered) SetHover(false);
        }

        public void SetMissingCard(bool missingCard)
        {
            isMissingCard = missingCard;
        }

        public void SetHover(bool state)
        {
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
            if (visualRoot == null) return;
        
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

            while (time < transitionDuration)
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
                    descriptionGroup.alpha = Mathf.Lerp(
                        startDescriptionAlpha,
                        targetDescriptionAlpha,
                        t
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
