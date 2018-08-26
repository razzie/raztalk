﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace raztalk.bot
{
    public abstract class Bot : IDisposable
    {
        private Dictionary<string, string> m_args = new Dictionary<string, string>();

        public delegate void NewMessageEvent(Bot bot, string message);
        public delegate void ArgChangedEvent(Bot bot, string arg, string value);

        public event NewMessageEvent NewMessage;
        public event ArgChangedEvent ArgChanged;

        public abstract string Name { get; }

        public string this[string arg]
        {
            get { return m_args[arg]; }
            set
            {
                m_args[arg] = value;
                ArgChanged?.Invoke(this, arg, value);
            }
        }

        public virtual void ConsumeMessage(string user, string message, DateTime timestamp)
        {
        }

        public virtual void Dispose()
        {
            m_args.Clear();
            NewMessage = null;
            ArgChanged = null;
        }
        
        private void FireNewMessage(string message)
        {
            NewMessage?.Invoke(this, message);
        }


        static public Type[] Bots { get; private set; }
        
        static Bot()
        {
            //Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            //Type[] types = assemblies.SelectMany(a => a.GetTypes()).ToArray();
            //Bots = types.Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Bot))).ToArray();
            ReloadBots();
        }

        static public void ReloadBots()
        {
            lock (Bots)
            {
                var bots = new List<Type>();
                var botdir = new DirectoryInfo("bots/");
                foreach (var file in botdir.GetFiles("*.dll"))
                {
                    var dll = Assembly.LoadFile(file.FullName);
                    bots.AddRange(dll.GetExportedTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Bot))));
                }

                Bots = bots.ToArray();
            }
        }

        static public Bot Create(string bot)
        {
            foreach (var botclass in Bots)
            {
                if (botclass.Name.Equals(bot))
                    return (Bot)Activator.CreateInstance(botclass);
            }

            return null;
        }
    }
}
