using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Diagnostics;

namespace COMessengerClient
{
    
    /// <summary>
    /// Обертка для SortedList, позволяющая не выбрасывать исключение в случае если указанного элемента нет в коллекции, а возвращать TKey
    /// </summary>
    /// <typeparam name="TKey">Тип ключа</typeparam>
    /// <typeparam name="TValue">Тип значения</typeparam>
    public class BackedSortedList<TKey, TValue>
    {
        private SortedList<string, string> back = new SortedList<string, string>();

        public string this[string key]
        {
            get 
            {
                //string tmp;

                if (back.ContainsKey(key))
                    return back[key];
                else
                    return key;

            }
            set
            {
                back[key] = value;
            }
        }

        public void Add(string key, string value)
        {
            back.Add(key, value);
        }

        public int Count { get { return back.Count; } }
    }

    /// <summary>
    /// Транслируемая запись
    /// </summary>
    [Serializable]
    public class LocalizationEntry
    {
        [XmlAttribute]
        public string Remark { get; set; }       //Комментарий

        public string Original { get; set; }     //Оригинальный текст
        public string Translation { get; set; }  //Перевод
    }

    /// <summary>
    /// Локализатор
    /// </summary>
    public class LocalizationUI : INotifyPropertyChanged
    {
        public LocalizationUI()
        {
            LocaleStrings = new BackedSortedList<string, string>();
        }

        private static XmlSerializer serializer = new XmlSerializer(typeof(List<LocalizationEntry>));
        
        /// <summary>
        /// Загрузить перевод
        /// </summary>
        /// <param name="info">Язык</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public void Load(CultureInfo info)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            List<LocalizationEntry> StringSet;

            LocaleStrings = new BackedSortedList<string, string>();

            string FilePath = Path.Combine("Localization", String.Concat(info.Name, ".xml"));

            if (!File.Exists(FilePath))
                FilePath = Path.Combine("Localization", String.Concat(info.TwoLetterISOLanguageName, ".xml"));

            if (File.Exists(FilePath))
            {
                using (Stream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (XmlReader xml_reader = new XmlTextReader(fs))
                {
                    StringSet = serializer.Deserialize(xml_reader) as List<LocalizationEntry>;
                }

                StringSet.ForEach(unit => LocaleStrings.Add(unit.Original, unit.Translation));

                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("LocaleStrings"));
                }
            }
        }

        /// <summary> 
        /// Хранилище строк перевода
        /// </summary>
        public BackedSortedList<string, string> LocaleStrings { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Спискота доступных переводов (по сути файликов в папке Localization)
        /// </summary>
        public static IEnumerable<CultureInfo> AvailableCultureInfo 
        {
            get
            {
                return CultureInfo.GetCultures(CultureTypes.AllCultures).Where(info =>
                    {
                        return File.Exists(Path.Combine("Localization", String.Concat(info.Name, ".xml")));
                    }).ToList();

            }
        }
    

    }
}
