using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace SimpleMendingYourself
{
    public class ITab_SelfMendPawnFilter : ITab
    {
        private Vector2 scrollPosition;

        private Building Bench => SelThing as Building;
        private CompSelfMendPawnFilter Comp => Bench?.TryGetComp<CompSelfMendPawnFilter>();

        public override bool IsVisible => Comp != null;

        public ITab_SelfMendPawnFilter()
        {
            labelKey = "SMY_PawnFilterTab";
            size = new Vector2(280f, 460f);
        }

        protected override void FillTab()
        {
            CompSelfMendPawnFilter comp = Comp;
            if (comp == null)
                return;

            List<Pawn> colonists = Bench.Map.mapPawns.FreeColonistsSpawned;
            Rect inRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 25f), "SMY_AllowedPawns".Translate());

            float btnY = inRect.y + 30f;
            float btnW = (inRect.width - 5f) / 2f;
            if (Widgets.ButtonText(new Rect(inRect.x, btnY, btnW, 24f), "SMY_SelectAll".Translate()))
                comp.AllowAll();
            if (Widgets.ButtonText(new Rect(inRect.x + btnW + 5f, btnY, btnW, 24f), "SMY_SelectNone".Translate()))
                comp.DisallowAll(colonists);

            float listY = btnY + 30f;
            float listHeight = inRect.yMax - listY;
            Rect listRect = new Rect(inRect.x, listY, inRect.width, listHeight);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, colonists.Count * 30f);

            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
            float y = 0f;
            foreach (Pawn pawn in colonists)
            {
                bool allowed = comp.IsPawnAllowed(pawn);
                bool prev = allowed;
                Widgets.CheckboxLabeled(new Rect(0f, y, viewRect.width, 28f), pawn.LabelShort, ref allowed);
                if (allowed != prev)
                    comp.SetPawnAllowed(pawn, allowed);
                y += 30f;
            }
            Widgets.EndScrollView();
        }
    }
}
