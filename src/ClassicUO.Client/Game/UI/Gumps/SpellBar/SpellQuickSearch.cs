using System;
using ClassicUO.Assets;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps.SpellBar;

public class SpellQuickSearch : NineSliceGump
{
    private TTFTextInputField searchField;
    private SpellDisplay spellDisplay;
    private Action<SpellDefinition>  onClick;
    private bool disposeOnReturn;

    public SpellQuickSearch(World world, int x, int y, Action<SpellDefinition> onClick = null, bool disposeOnReturn = false) : base(world, x, y, 200, 79, ModernUIConstants.ModernUIPanel, ModernUIConstants.ModernUIPanel_BoderSize, false)
    {
        CanMove = true;
        X = x;
        Y = y;
        this.disposeOnReturn = disposeOnReturn;
        this.onClick = onClick;

        Build();
    }

    public override bool AcceptMouseInput { get; set; } = true;

    private void Build()
    {
        Add(searchField = new TTFTextInputField(Width - 10, 25, Width - 10){Y = 49, X = 5});
        searchField.SetPlaceholder("Search..");
        searchField.SetFocus();

        searchField.TextChanged += SearchTextChanged;
        searchField.EnterPressed += SearchEnterPressed;

        Add(spellDisplay = new(Width, onClick, disposeOnReturn){Y = 5, X = 5});
    }

    private void SearchEnterPressed(object sender, EventArgs e)
    {
        if (IsDisposed)
            return;

        searchField.OnFocusLost();
        UIManager.KeyboardFocusControl = null;
        spellDisplay.InvokeAction();
        if(disposeOnReturn)
            Dispose();
    }

    private void SearchTextChanged(object sender, EventArgs e)
    {
        if (IsDisposed)
            return;

        if(searchField?.Text.Length > 0)
        {
            if (SpellDefinition.TryGetSpellFromName(searchField?.Text, out SpellDefinition spell))
                spellDisplay.SetSpell(spell);
        }
        else
        {
            spellDisplay.ClearSpell();
        }
    }

    private class SpellDisplay : Control
    {
        private GumpPic icon;
        private TextBox text;
        private Action<SpellDefinition> onClick;
        private bool disposeOnReturn;

        public SpellDefinition Spell;
        public SpellDisplay(int width, Action<SpellDefinition> onClick, bool disposeOnReturn)
        {
            Width = width;
            Height = 44;
            CanMove = true;
            this.onClick = onClick;
            this.disposeOnReturn = disposeOnReturn;
        }

        public override bool AcceptMouseInput { get; set; } = true;

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            if (button != MouseButtonType.Left) return;

            InvokeAction();

            if(disposeOnReturn)
                Parent?.Dispose();
        }

        public void InvokeAction() => onClick?.Invoke(Spell);

        public void ClearSpell()
        {
            Spell = null;
            icon?.Dispose();
            text?.Dispose();
        }

        public void SetSpell(SpellDefinition spell)
        {
            Spell = spell;
            icon?.Dispose();
            //Intential to only use height, these are square spell icons.
            Add(icon = new GumpPic(0, 0, (ushort)spell.GumpIconSmallID, 0) { AcceptMouseInput = false, Width = Height, Height = Height });
            Color color = Color.White;

            switch (spell.TargetType)
            {
                case TargetType.Harmful: color = Color.OrangeRed; break;

                case TargetType.Beneficial: color = Color.GreenYellow; break;
            }

            text?.Dispose();
            Add(text = TextBox.GetOne(spell.Name, TrueTypeLoader.EMBEDDED_FONT, 18, color, TextBox.RTLOptions.Default(Width - Height)));
            text.X = Height;
            text.Y = (Height - text.Height) >> 1;
        }
    }
}
