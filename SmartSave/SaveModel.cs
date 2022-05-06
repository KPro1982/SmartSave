using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace SmartSave
{
    public struct SavedGame : IEquatable<SavedGame>, IComparable<SavedGame>
    {
        internal int Index { get; set; }
        internal string CharacterName { get; set; }
        internal string World { get; set; }
        internal string Day { get; set; }
        internal string Hour { get; set; }
        internal string Path { get; set; }

        internal string FileName { get; set; }
        internal DateTime FileDate { get; set; }
        internal string DisplayName { get; set; }

        public SavedGame(int index, string path)
        {
            CharacterName = "";
            World = "";
            Day = "";
            Hour = "";
            DisplayName = "";
            FileDate = default;
            this.Index = index;
            this.Path = path;
            this.FileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string[] nameParts = FileName.Split('_');
            if (nameParts.Length == 4)
            {
                this.CharacterName = nameParts[0];
                this.World = nameParts[1];
                this.Day = nameParts[2];
                this.Hour = nameParts[3];
                this.FileDate = File.GetLastWriteTimeUtc(path);
                this.DisplayName = $"{Day}, Hour {Hour}";
            }
            else
            {
                Debug.LogError($"SmartSave parsing error: P {path} F {FileName}");
            }
        }

        public int CompareTo(SavedGame compareSg)
        {
            return this.FileDate.CompareTo(compareSg.FileDate);
        }

        public bool Equals(SavedGame other)
        {
            return (this.FileDate.Equals(other.FileDate));
        }
    }

    public class SaveModel
    {
        private string SavePath { get; set; }
        private List<SavedGame> SaveLibrary { get; set; }

        public SaveModel(string savePath)
        {
            this.SavePath = savePath;
            SaveLibrary = new List<SavedGame>();
            Refresh();
        }

        internal void Refresh()
        {
            SaveLibrary.Clear();
            int i = 0;
            foreach (string f in Directory.GetFiles(SavePath, "*.sav"))
            {
                i++;
                SaveLibrary.Add(new SavedGame(i, f));
            }

            SaveLibrary.Sort();
        }

        internal string[] GetSaveNames()
        {
            Refresh();
            List<string> fnames = new List<string>();
            foreach (SavedGame sg in SaveLibrary)
            {
                fnames.Add(sg.DisplayName);
            }

            return fnames.ToArray();
        }

        internal string[] GetSaveNames(string cName, string wName)
        {
            Refresh();
            List<string> fnames = new List<string>();
            foreach (SavedGame sg in SaveLibrary)
            {
                if (sg.CharacterName.ToLower() == cName.ToLower() &&
                    sg.World.ToLower() == wName.ToLower())
                {
                    fnames.Add(sg.DisplayName);
                }
            }

            return fnames.ToArray();
        }

        [CanBeNull]
        internal string GetPathFromDisplayName(string dName)
        {
            SavedGame sg = SaveLibrary.Find(s => s.DisplayName == dName);
            return sg.Path;
        }
    }
}