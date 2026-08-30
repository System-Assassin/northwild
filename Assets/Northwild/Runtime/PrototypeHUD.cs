using System.Collections.Generic;
using UnityEngine;

namespace Northwild
{
    public sealed class PrototypeHUD : MonoBehaviour
    {
        private NorthwildGame game;
        private GUIStyle titleStyle;
        private GUIStyle textStyle;
        private GUIStyle smallStyle;
        private GUIStyle centreStyle;
        private GUIStyle warningStyle;
        private GUIStyle panelStyle;
        private bool stylesReady;

        public void Configure(NorthwildGame northwildGame)
        {
            game = northwildGame;
        }

        private void PrepareStyles()
        {
            if (stylesReady)
                return;
            stylesReady = true;

            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(0.88f, 0.94f, 0.95f);
            textStyle = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            textStyle.normal.textColor = new Color(0.9f, 0.92f, 0.9f);
            smallStyle = new GUIStyle(textStyle) { fontSize = 12 };
            centreStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 15, fontStyle = FontStyle.Bold };
            warningStyle = new GUIStyle(centreStyle) { fontSize = 18 };
            warningStyle.normal.textColor = new Color(1f, 0.55f, 0.35f);
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = MakeTexture(new Color(0.035f, 0.055f, 0.06f, 0.88f));
        }

        private void OnGUI()
        {
            if (game == null || game.Vitals == null || game.Climate == null)
                return;
            PrepareStyles();

            DrawConditionPanel();
            DrawWorldPanel();
            DrawCrosshairAndPrompt();
            DrawContextHelp();
            DrawMessage();
            if (game.InventoryOpen)
                DrawInventory();
            if (game.Vitals.IsDead)
                DrawDeath();
        }

        private void DrawConditionPanel()
        {
            Rect panel = new Rect(18f, 18f, 305f, 255f);
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(34f, 29f, 220f, 28f), "NORTHWILD", titleStyle);

            float y = 67f;
            DrawBar("Health", game.Vitals.Health / 100f, game.Vitals.Health.ToString("0") + "%", new Color(0.63f, 0.18f, 0.14f), ref y);
            DrawBar("Core", Mathf.InverseLerp(32f, 40f, game.Vitals.CoreTemperatureC), game.Vitals.CoreTemperatureC.ToString("0.0") + " °C", TemperatureColour(), ref y);
            DrawBar("Hydration", game.Vitals.Hydration / 100f, game.Vitals.Hydration.ToString("0") + "%", new Color(0.18f, 0.48f, 0.72f), ref y);
            DrawBar("Calories", game.Vitals.Calories / 3200f, game.Vitals.Calories.ToString("0") + " kcal", new Color(0.69f, 0.43f, 0.13f), ref y);
            DrawBar("Energy", game.Vitals.Fatigue / 100f, game.Vitals.Fatigue.ToString("0") + "%", new Color(0.42f, 0.62f, 0.25f), ref y);
            DrawBar("Wetness", game.Vitals.Wetness / 100f, game.Vitals.Wetness.ToString("0") + "%", new Color(0.22f, 0.55f, 0.68f), ref y);
        }

        private void DrawBar(string label, float value, string valueText, Color colour, ref float y)
        {
            GUI.Label(new Rect(34f, y, 82f, 20f), label, smallStyle);
            Rect background = new Rect(112f, y + 3f, 128f, 13f);
            GUI.DrawTexture(background, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.12f, 0.15f, 0.16f), 0f, 0f);
            Rect fill = new Rect(background.x, background.y, background.width * Mathf.Clamp01(value), background.height);
            GUI.DrawTexture(fill, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, colour, 0f, 0f);
            GUI.Label(new Rect(247f, y - 1f, 70f, 20f), valueText, smallStyle);
            y += 28f;
        }

        private void DrawWorldPanel()
        {
            Rect panel = new Rect(Screen.width - 295f, 18f, 277f, 161f);
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 15f, panel.y + 12f, 230f, 25f), game.Climate.TimeLabel, titleStyle);
            GUI.Label(new Rect(panel.x + 15f, panel.y + 48f, 245f, 22f),
                game.Climate.Weather + "  |  " + game.Climate.AmbientTemperatureC.ToString("0.0") + " °C", textStyle);
            GUI.Label(new Rect(panel.x + 15f, panel.y + 74f, 245f, 22f),
                "Wind " + game.Climate.WindMetresPerSecond.ToString("0.0") + " m/s", textStyle);
            GUI.Label(new Rect(panel.x + 15f, panel.y + 100f, 245f, 22f),
                "Feels like " + game.Vitals.EffectiveTemperatureC.ToString("0.0") + " °C", textStyle);
            GUI.Label(new Rect(panel.x + 15f, panel.y + 126f, 245f, 22f),
                "Ground snow " + (game.World.SnowCover01 * 100f).ToString("0") + "%", textStyle);
        }

        private void DrawCrosshairAndPrompt()
        {
            float centreX = Screen.width * 0.5f;
            float centreY = Screen.height * 0.5f;
            GUI.Label(new Rect(centreX - 10f, centreY - 15f, 20f, 30f), "+", centreStyle);

            string prompt = game.Interaction != null ? game.Interaction.CurrentPrompt : string.Empty;
            if (!string.IsNullOrEmpty(prompt))
                GUI.Label(new Rect(centreX - 230f, centreY + 34f, 460f, 30f), "[E] " + prompt, centreStyle);
        }

        private void DrawContextHelp()
        {
            Campfire fire = Campfire.ClosestTo(game.Player.transform.position, 3.2f);
            float height = fire == null ? 76f : 110f;
            Rect panel = new Rect(18f, Screen.height - height - 18f, Mathf.Min(780f, Screen.width - 36f), height);
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 10f, panel.width - 28f, 22f),
                "WASD move  |  Shift sprint  |  C camera  |  E interact  |  Tab pack  |  B fire ring  |  H lean-to", smallStyle);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 34f, panel.width - 28f, 22f),
                "Near fire: T tinder  |  K kindling  |  L fuel  |  I ignite  |  P boil  |  R drink  |  O eat  |  F5 save  |  F9 load", smallStyle);
            if (fire != null)
                GUI.Label(new Rect(panel.x + 14f, panel.y + 65f, panel.width - 28f, 22f), "Nearby fire — " + fire.Status, textStyle);
#if UNITY_EDITOR
            GUI.Label(new Rect(panel.x + 14f, panel.y - 24f, panel.width - 28f, 22f),
                "HDRP effects: F6 cycle clouds/weather  |  F7 place a lit test fire", smallStyle);
#endif
        }

        private void DrawMessage()
        {
            if (Time.unscaledTime > game.MessageExpiresAt || string.IsNullOrEmpty(game.LatestMessage))
                return;
            GUI.Label(new Rect(Screen.width * 0.5f - 360f, Screen.height - 175f, 720f, 35f), game.LatestMessage, centreStyle);
        }

        private void DrawInventory()
        {
            Rect panel = new Rect(Screen.width * 0.5f - 245f, Screen.height * 0.5f - 260f, 490f, 520f);
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 20f, 300f, 30f), "RUCKSACK", titleStyle);
            GUI.Label(new Rect(panel.x + 310f, panel.y + 24f, 155f, 24f),
                game.Inventory.CurrentWeightKg.ToString("0.0") + " / " + game.Inventory.MaximumWeightKg.ToString("0") + " kg", textStyle);

            List<InventoryEntry> entries = game.Inventory.Capture();
            float y = panel.y + 72f;
            if (entries.Count == 0)
                GUI.Label(new Rect(panel.x + 24f, y, 420f, 24f), "Empty. Gather only what you can carry.", textStyle);
            foreach (InventoryEntry entry in entries)
            {
                GUI.Label(new Rect(panel.x + 28f, y, 260f, 24f), ItemCatalog.DisplayName(entry.item), textStyle);
                GUI.Label(new Rect(panel.x + 292f, y, 55f, 24f), "× " + entry.amount, textStyle);
                float weight = ItemCatalog.WeightKg(entry.item) * entry.amount;
                GUI.Label(new Rect(panel.x + 365f, y, 90f, 24f), weight.ToString("0.00") + " kg", smallStyle);
                y += 29f;
            }
            GUI.Label(new Rect(panel.x + 24f, panel.yMax - 42f, 420f, 24f), "Press Tab to close", smallStyle);
        }

        private void DrawDeath()
        {
            GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, panelStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 280f, Screen.height * 0.5f - 55f, 560f, 45f), "YOU DID NOT SURVIVE", warningStyle);
            GUI.Label(new Rect(Screen.width * 0.5f - 280f, Screen.height * 0.5f, 560f, 35f), "Press F9 to load your last save.", centreStyle);
        }

        private Color TemperatureColour()
        {
            if (game.Vitals.CoreTemperatureC < 35f) return new Color(0.2f, 0.48f, 0.8f);
            if (game.Vitals.CoreTemperatureC > 38.5f) return new Color(0.84f, 0.28f, 0.15f);
            return new Color(0.36f, 0.66f, 0.42f);
        }

        private static Texture2D MakeTexture(Color colour)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, colour);
            texture.Apply();
            return texture;
        }
    }
}
