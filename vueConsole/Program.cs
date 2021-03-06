﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaseDonnees;
using Rechercheur;
using proxy;
using System.Runtime.InteropServices;
using Configurateur;

namespace vueConsole
{
    /// <summary>
    /// Classe de base du programme
    /// </summary>
    class Program
    {
        //private static readonly IEproxy Controller = new Eproxy();

        /// <summary>
        /// On appelle le configurateur
        /// </summary>
        private static Configurateur.Configurateur conf = new Configurateur.Configurateur();

        /// <summary>
        /// Point d'entrée du programme
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            NativeMethods.Handler = ConsoleEventCallback;
            NativeMethods.SetConsoleCtrlHandler(NativeMethods.Handler, true);

            try
            {
                Console.WriteLine(System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]));

                conf.init();

                conf.Demarrage();

                /*DAL d = new DAL(System.IO.Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]) + "/bdd/test.sqlite");

                d.suppressionDB();
                d.creation();
                d.seed();

                Rechercheur.Rechercheur r = new Rechercheur.Rechercheur(d);

                Controller.setRechercheur(r);
                r.ValArret = 20;

                Controller.StartProxy();*/

                Console.WriteLine("Hit any key to exit..");
                Console.WriteLine();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
          
            Console.ReadKey();

            //Controller.Stop();
            conf.Stop();
        }

        private static bool ConsoleEventCallback(int eventType)
        {
            if (eventType != 2) return false;
            try
            {
                //Controller.Stop();
                conf.Stop();
            }
            catch
            {
                // ignored
            }
            return false;
        }
    }

    internal static class NativeMethods
    {
        // Keeps it from getting garbage collected
        internal static ConsoleEventDelegate Handler;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        // Pinvoke
        internal delegate bool ConsoleEventDelegate(int eventType);
    }
}
