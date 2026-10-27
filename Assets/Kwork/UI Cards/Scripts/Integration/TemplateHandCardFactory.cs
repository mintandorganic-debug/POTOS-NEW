using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardCarouselUI
{
    public class TemplateHandCardFactory : IHandCardFactory
    {
        private readonly IReadOnlyDictionary<HandCardCategory, RectTransform> templates;
        private readonly RectTransform fallbackTemplate;

        public TemplateHandCardFactory(
            IReadOnlyDictionary<HandCardCategory, RectTransform> templates,
            RectTransform fallbackTemplate)
        {
            this.templates = templates;
            this.fallbackTemplate = fallbackTemplate;
        }

        public RectTransform Create(
            HandCardInfo info,
            RectTransform parent,
            CardCarouselController carousel,
            Action afterActivation)
        {
            RectTransform card =
                UnityEngine.Object.Instantiate(ResolveTemplate(info.Category), parent);

            card.name = info.Id;

            CardFaceArt.Apply(card, info.Art);

            if (card.TryGetComponent(out CardHover hover))
            {
                hover.Bind(carousel);
                hover.SetDescription(info.Description);

                if (info.HoverChanged != null)
                    hover.HoverChanged += info.HoverChanged;

                hover.Clicked += () =>
                {
                    afterActivation();
                    info.Activated?.Invoke();
                };
            }

            return card;
        }

        private RectTransform ResolveTemplate(HandCardCategory category)
        {
            return templates.TryGetValue(category, out RectTransform template) && template != null
                ? template
                : fallbackTemplate;
        }
    }
}
