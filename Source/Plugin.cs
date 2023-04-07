using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace MoreTranslations
{
    [BepInPlugin("MoreTranslations_DontTouchFranky", "MoreTranslations", "1.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        private static Dictionary<string, Dictionary<string, string>> TextStrings;
        private static Dictionary<string, Dictionary<string, string>> TextKeynotes;

        private static TMP_Dropdown languagesDropdown = null;
        private static string selectedLanguage = null;
        private static List<String> languages = new List<String>();

        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Plugin));
        }

        [HarmonyPatch(typeof(GameManager), "Start"), HarmonyPrefix]
        static void Start()
        {
            selectedLanguage = PlayerPrefs.GetString("linguaSelezionata");

            TextStrings = new Dictionary<string, Dictionary<string, string>>((IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
            TextKeynotes = new Dictionary<string, Dictionary<string, string>>((IEqualityComparer<string>)StringComparer.OrdinalIgnoreCase);
            if (selectedLanguage != "")
            {
                Debug.Log("MoreTranslations: " + selectedLanguage);

                TextStrings[selectedLanguage] = new Dictionary<string, string>();
                TextKeynotes[selectedLanguage] = new Dictionary<string, string>();
            }
            else
            {
                Debug.Log("MoreTranslations: Default");
            }

            CreateLanguageDropdown();
            GetTranslationLanguages();

            // Se la lingua selezionata non è presente tra quelle disponibili, la imposto a default
            if (selectedLanguage != "" && !languages.Contains(selectedLanguage))
            {
                selectedLanguage = "";
                PlayerPrefs.SetString("linguaSelezionata", selectedLanguage);
            }

            languagesDropdown.options.Add(new TMP_Dropdown.OptionData("Default"));

            // Aggiungo le lingue al dropdown
            foreach (String lingua in languages)
            {
                languagesDropdown.options.Add(new TMP_Dropdown.OptionData(Capitalize(lingua.ToLower())));
            }

            if (selectedLanguage == "")
            {
                languagesDropdown.value = 0;
            }
            else
            {
                languagesDropdown.value = languagesDropdown.options.FindIndex(x => x.text.ToLower() == selectedLanguage.ToLower());
            }

            languagesDropdown.onValueChanged.AddListener((value) =>
            {
                String selectedLanguage = languagesDropdown.options[value].text;
                if (selectedLanguage == "Default")
                {
                    selectedLanguage = "";
                }

                PlayerPrefs.SetString("linguaSelezionata", selectedLanguage.ToLower());
                AlertManager.Instance.AlertConfirm(Texts.Instance.GetText("selectLanguageChanged"));
            });
        }

        static void CreateLanguageDropdown()
        {
            // Duplico il dropdown delle lingue
            TMP_Dropdown originalDropdown = SettingsManager.Instance.languageDropdown;
            TMP_Dropdown clonedDropdown = Instantiate(originalDropdown, originalDropdown.transform.parent);
            clonedDropdown.transform.SetSiblingIndex(originalDropdown.transform.GetSiblingIndex() + 1);
            clonedDropdown.name = "languageDropdown2";
            clonedDropdown.onValueChanged.RemoveAllListeners();
            clonedDropdown.onValueChanged = new TMP_Dropdown.DropdownEvent();

            // Sposto il dropdown delle lingue a destra del primo 
            RectTransform rectTransform = clonedDropdown.GetComponent<RectTransform>();
            double width = rectTransform.rect.width;
            rectTransform.anchoredPosition = new Vector2((float)(rectTransform.anchoredPosition.x + width + 10), rectTransform.anchoredPosition.y);

            // Svuoto il dropdown delle lingue
            clonedDropdown.ClearOptions();

            languagesDropdown = clonedDropdown;
        }

        static void GetTranslationLanguages()
        {
            // Recupero tutte le cartelle nella cartella translations
            string path = "BepInEx/plugins/MoreTranslations_DontTouchFranky/translations";
            if (!Directory.Exists(path))
            {
                Debug.LogError("MoreTranslations: translations folder not found!");
                return;
            }

            string[] foldersArray = Directory.GetDirectories(path);

            List<string> folderNames = new List<string>();
            for (int i = 0; i < foldersArray.Length; i++)
            {
                String nomeCartella = Path.GetFileName(foldersArray[i]);
                if (nomeCartella[0] == '_')
                {
                    continue;
                }
                folderNames.Add(Path.GetFileName(foldersArray[i]));
            }

            // Aggiungo le lingue al dizionario
            foreach (String language in folderNames)
            {
                languages.Add(language);
            }
        }

        static String Capitalize(string str)
        {
            if (str == null || str.Length < 1)
                return str;

            return char.ToUpper(str[0]) + str.Substring(1);
        }

        [HarmonyPatch(typeof(Texts), "GetText"), HarmonyPrefix]
        static bool GetTextPrefix(string _id, string _type, ref string __result)
        {
            __result = "";

            if ((UnityEngine.Object)Globals.Instance == (UnityEngine.Object)null || !GameManager.Instance.PrefsLoaded)
            {
                __result = "";
                return false;
            }

            string id = _id.Replace(" ", "").ToLower();
            if (!(id != ""))
            {
                __result = "";
                return false;
            }

            if (TextStrings != null && TextStrings.ContainsKey(selectedLanguage))
            {
                if (_type != "")
                    id = _type.ToLower() + "_" + id.ToLower();

                if (TextStrings[selectedLanguage].ContainsKey(id))
                {
                    string testo = TextStrings[selectedLanguage][id];

                    if (testo != "")
                    {
                        {
                            __result = testo;
                            return false;
                        }
                    }
                }
            }

            return true;
        }


        [HarmonyPatch(typeof(Texts), "LoadTranslationText"), HarmonyPrefix]
        static void LoadTranslationTextPrefix(string type)
        {
            if (selectedLanguage != "")
            {
                string path = "";
                string[] lines = null;
                type = type.ToLower();
                switch (type)
                {
                    case "":
                        path = "BepInEx/plugins/MoreTranslations_DontTouchFranky/translations/" + selectedLanguage + "/" + selectedLanguage + ".txt";
                        if (File.Exists(path))
                            lines = File.ReadAllLines(path);
                        break;
                    case "keynotes":
                        path = "BepInEx/plugins/MoreTranslations_DontTouchFranky/translations/" + selectedLanguage + "/" + selectedLanguage + "_keynotes.txt";
                        if (File.Exists(path))
                            lines = File.ReadAllLines(path);
                        break;
                    case "traits":
                        path = "BepInEx/plugins/MoreTranslations_DontTouchFranky/translations/" + selectedLanguage + "/" + selectedLanguage + "_traits.txt";
                        if (File.Exists(path))
                            lines = File.ReadAllLines(path);
                        break;
                    case "auracurse":
                        path = "BepInEx/plugins/MoreTranslations_DontTouchFranky/translations/" + selectedLanguage + "/" + selectedLanguage + "_auracurse.txt";
                        if (File.Exists(path))
                            lines = File.ReadAllLines(path);
                        break;
                    case "events":
                        path = "BepInEx/plugins/MoreTranslations_DontTouchFranky/translations/" + selectedLanguage + "/" + selectedLanguage + "_events.txt";
                        if (File.Exists(path))
                            lines = File.ReadAllLines(path);
                        break;
                    case "nodes":
                        path = "BepInEx/plugins/MoreTranslations_DontTouchFranky/translations/" + selectedLanguage + "/" + selectedLanguage + "_nodes.txt";
                        if (File.Exists(path))
                            lines = File.ReadAllLines(path);
                        break;
                    case "cards":
                        path = "BepInEx/plugins/MoreTranslations_DontTouchFranky/translations/" + selectedLanguage + "/" + selectedLanguage + "_cards.txt";
                        if (File.Exists(path))
                            lines = File.ReadAllLines(path);
                        break;
                    case "fluff":
                        path = "BepInEx/plugins/MoreTranslations_DontTouchFranky/translations/" + selectedLanguage + "/" + selectedLanguage + "_cardsfluff.txt";
                        if (File.Exists(path))
                            lines = File.ReadAllLines(path);
                        break;
                    case "class":
                        path = "BepInEx/plugins/MoreTranslations_DontTouchFranky/translations/" + selectedLanguage + "/" + selectedLanguage + "_class.txt";
                        if (File.Exists(path))
                            lines = File.ReadAllLines(path);
                        break;
                    case "monsters":
                        path = "BepInEx/plugins/MoreTranslations_DontTouchFranky/translations/" + selectedLanguage + "/" + selectedLanguage + "_monsters.txt";
                        if (File.Exists(path))
                            lines = File.ReadAllLines(path);
                        break;
                    case "requirements":
                        path = "BepInEx/plugins/MoreTranslations_DontTouchFranky/translations/" + selectedLanguage + "/" + selectedLanguage + "_requirements.txt";
                        if (File.Exists(path))
                            lines = File.ReadAllLines(path);
                        break;
                    case "tips":
                        path = "BepInEx/plugins/MoreTranslations_DontTouchFranky/translations/" + selectedLanguage + "/" + selectedLanguage + "_tips.txt";
                        if (File.Exists(path))
                            lines = File.ReadAllLines(path);
                        break;
                }

                if (lines != null)
                {
                    List<string> stringList = new List<string>(lines);

                    int num = 0;
                    StringBuilder stringBuilder1 = new StringBuilder();
                    StringBuilder stringBuilder2 = new StringBuilder();

                    for (int index = 0; index < stringList.Count; ++index)
                    {
                        string str2 = stringList[index];
                        if (!(str2 == "") && str2[0] != '#')
                        {
                            string[] strArray = str2.Trim().Split(new char[1] { '=' }, 2);

                            if (strArray != null && strArray.Length >= 2)
                            {
                                strArray[0] = strArray[0].Trim().ToLower();
                                strArray[1] = Functions.SplitString("//", strArray[1])[0].Trim();
                                switch (type)
                                {
                                    case "keynotes":
                                        stringBuilder1.Append("keynotes_");
                                        break;
                                    case "traits":
                                        stringBuilder1.Append("traits_");
                                        break;
                                    case "auracurse":
                                        stringBuilder1.Append("auracurse_");
                                        break;
                                    case "events":
                                        stringBuilder1.Append("events_");
                                        break;
                                    case "nodes":
                                        stringBuilder1.Append("nodes_");
                                        break;
                                    case "cards":
                                    case "fluff":
                                        stringBuilder1.Append("cards_");
                                        break;
                                    case "class":
                                        stringBuilder1.Append("class_");
                                        break;
                                    case "monsters":
                                        stringBuilder1.Append("monsters_");
                                        break;
                                    case "requirements":
                                        stringBuilder1.Append("requirements_");
                                        break;
                                    case "tips":
                                        stringBuilder1.Append("tips_");
                                        break;
                                }

                                stringBuilder1.Append(strArray[0]);

                                if (TextStrings[selectedLanguage].ContainsKey(stringBuilder1.ToString()))
                                    TextStrings[selectedLanguage][stringBuilder1.ToString()] = strArray[1];
                                else
                                    TextStrings[selectedLanguage].Add(stringBuilder1.ToString(), strArray[1]);

                                bool flag = true;
                                if (type == "")
                                {
                                    if (strArray[1].StartsWith("rptd_", StringComparison.OrdinalIgnoreCase))
                                    {
                                        stringBuilder2.Append(strArray[1].Substring(5).ToLower());
                                        TextStrings[selectedLanguage][stringBuilder1.ToString()] = TextStrings[selectedLanguage][stringBuilder2.ToString()];
                                        flag = false;
                                        stringBuilder2.Clear();
                                    }
                                }
                                else if (type == "events")
                                {
                                    if (strArray[1].StartsWith("rptd_", StringComparison.OrdinalIgnoreCase))
                                    {
                                        stringBuilder2.Append("events_");
                                        stringBuilder2.Append(strArray[1].Substring(5).ToLower());
                                        TextStrings[selectedLanguage][stringBuilder1.ToString()] = TextStrings[selectedLanguage][stringBuilder2.ToString()];
                                        flag = false;
                                        stringBuilder2.Clear();
                                    }
                                }
                                else if (type == "cards")
                                {
                                    if (strArray[1].StartsWith("rptd_", StringComparison.OrdinalIgnoreCase))
                                    {
                                        stringBuilder2.Append("cards_");
                                        stringBuilder2.Append(strArray[1].Substring(5).ToLower());
                                        TextStrings[selectedLanguage][stringBuilder1.ToString()] = TextStrings[selectedLanguage][stringBuilder2.ToString()];
                                        flag = false;
                                        stringBuilder2.Clear();
                                    }
                                }
                                else if (type == "monsters" && strArray[1].StartsWith("rptd_", StringComparison.OrdinalIgnoreCase))
                                {
                                    stringBuilder2.Append("monsters_");
                                    stringBuilder2.Append(strArray[1].Substring(5).ToLower());
                                    TextStrings[selectedLanguage][stringBuilder1.ToString()] = TextStrings[selectedLanguage][stringBuilder2.ToString()];
                                    flag = false;
                                    stringBuilder2.Clear();
                                }

                                if (flag)
                                {
                                    string str3 = Regex.Replace(Regex.Replace(strArray[1], "<(.*?)>", ""), "\\s+", " ");
                                    num += str3.Split(' ').Length;
                                }
                                stringBuilder1.Clear();
                            }
                        }
                    }
                }
            }
        }
    }
}
