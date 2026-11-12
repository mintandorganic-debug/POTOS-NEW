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
            global::GameLog.Write(
                "TemplateHandCardFactory",
                $"Constructor templates={(templates != null ? templates.Count : 0)} fallbackTemplate={(fallbackTemplate != null ? fallbackTemplate.name : "NULL")}");

            this.templates = templates;
            this.fallbackTemplate = fallbackTemplate;
        }

        public RectTransform Create(
            HandCardInfo info,
            RectTransform parent,
            CardCarouselController carousel,
            Action afterActivation)
        {
            global::GameLog.Write(
                "TemplateHandCardFactory",
                $"Create id={info.Id} category={info.Category} parent={(parent != null ? parent.name : "NULL")} hasArt={info.Art != null}");

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
                    global::GameLog.Write(
                        "TemplateHandCardFactory",
                        $"Card clicked in carousel id={info.Id} category={info.Category}");

                    afterActivation();
                    info.Activated?.Invoke();
                };

                global::GameLog.Write(
                    "TemplateHandCardFactory",
                    $"Create bound id={info.Id} hasHoverCallback={info.HoverChanged != null} hasActivated={info.Activated != null}");
            }
            else
            {
                global::GameLog.Write(
                    "TemplateHandCardFactory",
                    $"Create template has no CardHover id={info.Id}, no input bound");
            }

            return card;
        }

        private RectTransform ResolveTemplate(HandCardCategory category)
        {
            global::GameLog.Write(
                "TemplateHandCardFactory",
                $"ResolveTemplate category={category} hasCategoryTemplate={(templates != null && templates.ContainsKey(category))}");

            return templates.TryGetValue(category, out RectTransform template) && template != null
                ? template
                : fallbackTemplate;
        }
    }
}
