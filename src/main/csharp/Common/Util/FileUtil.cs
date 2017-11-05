using System;
using System.Globalization;
using System.Threading;
using Shell32;

namespace SebastianHaeni.ThermoBox.Common.Util
{
    public static class FileUtil
    {
        /// <summary>
        /// Size array containing all abbreviations.
        /// Longs run out around EB, so no need to cover more.
        /// </summary>
        private static readonly string[] Sizes = {"B", "KB", "MB", "GB", "TB", "PB", "EB"};

        /// <summary>
        /// Gets a human readable byte count representation.
        /// </summary>
        public static string GetSizeRepresentation(ulong byteCount)
        {
            if (byteCount == 0)
            {
                return "0" + Sizes[0];
            }

            var place = Convert.ToInt32(Math.Floor(Math.Log(byteCount, 1024)));
            var num = Math.Round(byteCount / Math.Pow(1024, place), 1);

            return num.ToString(CultureInfo.InvariantCulture) + Sizes[place];
        }

        /// <summary>
        /// Moves a file into the Windows Recycle bin. Use the settings on the recycle bin
        /// to define the size limit of the recycle bin folder.
        /// </summary>
        public static void SendToTrash(string filepath)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                SendToTrashInternal(filepath);
            }
            else
            {
                var staThread = new Thread(SendToTrashInternal);
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start(filepath);
                staThread.Join();
            }
        }

        /// <summary>
        /// Ugh! This runs script tasks on MTA threads but Shell32 only wants to 
        /// run on STA thread. So start a new STA thread to call new Shell, block 
        /// till it's done, then return. 
        /// We use Shell32 since .net doesn't have Recycling Bin interface and we 
        /// prefer not to ship other dlls as they normally need to be deployed to 
        /// the GAC. So this is easiest, although not very pretty.
        /// </summary>
        private static void SendToTrashInternal(object filepath)
        {
            // Reference to shell instance.
            var shell = new Shell();

            // Reference to recycling bin folder. No correlation with the service Bitbucket.
            var recyclingBin = shell.NameSpace(ShellSpecialFolderConstants.ssfBITBUCKET);

            // Move
            recyclingBin.MoveHere(filepath);
        }
    }
}
