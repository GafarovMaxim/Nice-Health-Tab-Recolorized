using System;
using UnityEngine;
using Verse;

namespace NiceHealthTab_YAPEM_Patch
{
    public class Dialog_InputText : Window
    {
        private readonly string title;
        private string curText;
        private readonly Action<string> onAccept;

        public override Vector2 InitialSize => new Vector2(380f, 150f);

        public Dialog_InputText(string title, string defaultText, Action<string> onAccept)
        {
            this.title = title;
            this.curText = defaultText ?? "";
            this.onAccept = onAccept;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.doCloseX = true;
            this.forcePause = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width - 30f, 30f), title);
            Text.Font = GameFont.Small;

            Rect textRect = new Rect(0f, 38f, inRect.width, 28f);
            curText = Widgets.TextField(textRect, curText);

            float btnW = 90f;
            float btnH = 30f;
            float yBtn = inRect.height - btnH - 4f;

            Rect okRect = new Rect(inRect.width - btnW * 2f - 10f, yBtn, btnW, btnH);
            bool enterPressed = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return;

            if (Widgets.ButtonText(okRect, "OK".Translate()) || enterPressed)
            {
                if (!string.IsNullOrEmpty(curText?.Trim()))
                {
                    onAccept?.Invoke(curText.Trim());
                    Close();
                    if (enterPressed) Event.current.Use();
                }
            }

            Rect cancelRect = new Rect(inRect.width - btnW, yBtn, btnW, btnH);
            if (Widgets.ButtonText(cancelRect, "Cancel".Translate()))
            {
                Close();
            }
        }
    }
}
