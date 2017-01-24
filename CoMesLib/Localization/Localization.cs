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
using CorporateMessengerLibrary.Collections;

namespace CorporateMessengerLibrary.Localization
{


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
        /// <summary> 
        /// Хранилище строк перевода
        /// </summary>
        public BackedSortedList<string> LocaleStrings { get; private set; }

        public LocalizationUI()
        {
            LocaleStrings = new BackedSortedList<string>();
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

            //LocaleStrings = new BackedSortedList<string>();

            LocaleStrings.Clear();

            string FilePath = Path.Combine("Language", String.Concat(info.Name, ".xml"));

            if (!File.Exists(FilePath))
                FilePath = Path.Combine("Language", String.Concat(info.TwoLetterISOLanguageName, ".xml"));

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

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Спискота доступных переводов (по сути файликов в папке Language)
        /// </summary>
        public static IEnumerable<CultureInfo> AvailableCultureInfo 
        {
            get
            {
                return CultureInfo.GetCultures(CultureTypes.AllCultures).Where(info =>
                    {
                        return File.Exists(Path.Combine("Language", String.Concat(info.Name, ".xml")));
                    }).ToList();

            }
        }
    

    }
}
