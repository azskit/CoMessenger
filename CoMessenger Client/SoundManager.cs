using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Media;
using System.IO;

namespace COMessengerClient
{
    public class SoundManager
    {
        private SortedList<string, SoundPlayer> sounds = new SortedList<string,SoundPlayer>();

        public SoundManager()
        {
            InitSoundManager();
        }


        public void InitSoundManager ()
        {
            AddSoundPlayer("NewMessage",  Properties.Settings.Default.SoundFileNewMessage);
        }

        void AddSoundPlayer(string name, string file)
        {
            //SoundPlayer newSoundPlayer = new SoundPlayer();

            if (!File.Exists(file))
                return;

            //newSoundPlayer.SoundLocation = file;

            //newSoundPlayer.LoadAsync();

            //sounds.Add(name, newSoundPlayer);

            sounds.Add(name, new SoundPlayer(file));
            sounds[name].LoadAsync();
        }

        public void Play(string name)
        {
            SoundPlayer player = sounds[name];

            if (player.IsLoadCompleted)
                player.Play();
        }


    }
}
