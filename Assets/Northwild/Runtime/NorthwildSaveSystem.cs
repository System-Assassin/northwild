using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Northwild
{
    [Serializable]
    public class NorthwildSaveData
    {
        public Vector3 playerPosition;
        public Vector3 playerEulerAngles;
        public VitalsData vitals;
        public List<InventoryEntry> inventory;
        public int day;
        public float timeOfDay;
        public WeatherType weather;
        public List<CampfireData> campfires;
        public List<ShelterData> shelters;
    }

    public sealed class NorthwildSaveSystem : MonoBehaviour
    {
        private NorthwildGame game;
        private string SavePath { get { return Path.Combine(Application.persistentDataPath, "northwild-save.json"); } }

        public void Configure(NorthwildGame northwildGame)
        {
            game = northwildGame;
        }

        private void Update()
        {
            if (game == null)
                return;
            if (Input.GetKeyDown(KeyCode.F5))
                Save();
            if (Input.GetKeyDown(KeyCode.F9))
                Load();
        }

        private void Save()
        {
            NorthwildSaveData data = new NorthwildSaveData
            {
                playerPosition = game.Player.transform.position,
                playerEulerAngles = game.Player.transform.eulerAngles,
                vitals = game.Vitals.Capture(),
                inventory = game.Inventory.Capture(),
                day = game.Climate.Day,
                timeOfDay = game.Climate.TimeOfDay,
                weather = game.Climate.Weather,
                campfires = Campfire.CaptureAll(),
                shelters = Shelter.CaptureAll()
            };

            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
                game.Notify("Game saved.");
            }
            catch (Exception exception)
            {
                game.Notify("Save failed: " + exception.Message);
            }
        }

        private void Load()
        {
            if (!File.Exists(SavePath))
            {
                game.Notify("No Northwild save exists yet.");
                return;
            }

            try
            {
                NorthwildSaveData data = JsonUtility.FromJson<NorthwildSaveData>(File.ReadAllText(SavePath));
                CharacterController controller = game.Player.GetComponent<CharacterController>();
                controller.enabled = false;
                game.Player.transform.position = data.playerPosition;
                game.Player.transform.eulerAngles = data.playerEulerAngles;
                controller.enabled = true;
                game.Vitals.Restore(data.vitals);
                game.Inventory.Restore(data.inventory);
                game.Climate.Restore(data.day, data.timeOfDay, data.weather);
                Campfire.RestoreAll(data.campfires);
                Shelter.RestoreAll(data.shelters);
                game.Notify("Last save loaded.");
            }
            catch (Exception exception)
            {
                game.Notify("Load failed: " + exception.Message);
            }
        }
    }
}

