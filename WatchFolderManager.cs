#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2023 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ShareX
{
    /// <summary>
    /// Class <c>WatchFolderManager</c> models an entry point to manage the folders for the screeenshots.
    /// </summary>
    public class WatchFolderManager : IDisposable
    {
        /// <summary>
        /// The property <c>WatchFolders</c> represents a list of already registered WatchFolders.
        /// </summary>
        public List<WatchFolder> WatchFolders { get; private set; }

        /// <summary>
        /// Updates WatchFolders <see cref="WatchFolders"/> list with a new instances from <c>Program.DefaultTaskSettings.WatchFolderList</c>
        /// and <c>Program.HotkeysConfig.Hotkeys</c> lists.
        /// </summary>
        /// <example>For example:
        /// <code>
        /// WatchFolderManager manager = new WatchFolderManager();
        /// manager.UpdateWatchFolders(settings);
        /// </code>
        public void UpdateWatchFolders()
        {
            if (WatchFolders != null)
            {
                UnregisterAllWatchFolders();
            }

            WatchFolders = new List<WatchFolder>();

            foreach (WatchFolderSettings defaultWatchFolderSetting in Program.DefaultTaskSettings.WatchFolderList)
            {
                AddWatchFolder(defaultWatchFolderSetting, Program.DefaultTaskSettings);
            }

            foreach (HotkeySettings hotkeySetting in Program.HotkeysConfig.Hotkeys)
            {
                foreach (WatchFolderSettings watchFolderSetting in hotkeySetting.TaskSettings.WatchFolderList)
                {
                    AddWatchFolder(watchFolderSetting, hotkeySetting.TaskSettings);
                }
            }
        }

        private WatchFolder FindWatchFolder(WatchFolderSettings watchFolderSetting)
        {
            return WatchFolders.FirstOrDefault(watchFolder => watchFolder.Settings == watchFolderSetting);
        }

        private bool IsExist(WatchFolderSettings watchFolderSetting)
        {
            return FindWatchFolder(watchFolderSetting) != null;
        }

        /// <summary>
        /// Registers new <c>WatchFolder</c> instance.
        /// </summary>
        /// <param> <c>watchFolderSetting</c> is the WatchFolder properties to add.</param>
        /// <param> <c>taskSettings</c> is the settings that can be applied for the files in the folder.</param>
        public void AddWatchFolder(WatchFolderSettings watchFolderSetting, TaskSettings taskSettings)
        {
            if (!IsExist(watchFolderSetting))
            {
                if (!taskSettings.WatchFolderList.Contains(watchFolderSetting))
                {
                    taskSettings.WatchFolderList.Add(watchFolderSetting);
                }

                WatchFolder watchFolder = new WatchFolder();
                watchFolder.Settings = watchFolderSetting;
                watchFolder.TaskSettings = taskSettings;

                watchFolder.FileWatcherTrigger += origPath =>
                {
                    TaskSettings taskSettingsCopy = TaskSettings.GetSafeTaskSettings(taskSettings);
                    string destPath = origPath;

                    if (watchFolderSetting.MoveFilesToScreenshotsFolder)
                    {
                        string screenshotsFolder = TaskHelpers.GetScreenshotsFolder(taskSettingsCopy);
                        string fileName = Path.GetFileName(origPath);
                        destPath = TaskHelpers.HandleExistsFile(screenshotsFolder, fileName, taskSettingsCopy);
                        FileHelpers.CreateDirectoryFromFilePath(destPath);
                        File.Move(origPath, destPath);
                    }

                    UploadManager.UploadFile(destPath, taskSettingsCopy);
                };

                WatchFolders.Add(watchFolder);

                if (taskSettings.WatchFolderEnabled)
                {
                    watchFolder.Enable();
                }
            }
        }

        /// <summary>
        /// Removes the given WatchFolder from the list if it exists. <see cref="WatchFolders"/>
        /// </summary>
        /// <example>For example:
        /// <code>
        /// WatchFolderManager manager = new WatchFolderManager();
        /// WatchFolderSettings settings = new WatchFolderSettings {FolderPath = "./MyFolder"};
        /// manager.RemoveWatchFolder(settings);
        /// </code>
        /// <param><c>watchFolderSetting</c> is the WatchFolder properties to remove.</param>
        public void RemoveWatchFolder(WatchFolderSettings watchFolderSetting)
        {
            using (WatchFolder watchFolder = FindWatchFolder(watchFolderSetting))
            {
                if (watchFolder != null)
                {
                    watchFolder.TaskSettings.WatchFolderList.Remove(watchFolderSetting);
                    WatchFolders.Remove(watchFolder);
                }
            }
        }

        /// <summary>
        /// Updates the settings for the WatchFolder.
        /// </summary>
        /// <example>For example:
        /// <code>
        /// WatchFolderManager manager = new WatchFolderManager();
        /// WatchFolderSettings settings = new WatchFolderSettings {FolderPath = "./MyFolder"};
        /// manager.UpdateWatchFolderState(settings);
        /// </code>
        /// <param><c>watchFolderSetting</c> is the WatchFolder properties to update.</param>
        public void UpdateWatchFolderState(WatchFolderSettings watchFolderSetting)
        {
            WatchFolder watchFolder = FindWatchFolder(watchFolderSetting);
            if (watchFolder != null)
            {
                if (watchFolder.TaskSettings.WatchFolderEnabled)
                {
                    watchFolder.Enable();
                }
                else
                {
                    watchFolder.Dispose();
                }
            }
        }

        /// <summary>
        /// Unregisters all WatchFolders in the list. <see cref="WatchFolders"/>
        /// </summary>
        /// <example>For example:
        /// <code>
        /// WatchFolderManager manager = new WatchFolderManager();
        /// manager.UnregisterAllWatchFolders();
        /// </code>
        public void UnregisterAllWatchFolders()
        {
            if (WatchFolders != null)
            {
                foreach (WatchFolder watchFolder in WatchFolders)
                {
                    if (watchFolder != null)
                    {
                        watchFolder.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// Unregisters all WatchFolders. <see cref="UnregisterAllWatchFolders"/>
        /// </summary>
        public void Dispose()
        {
            UnregisterAllWatchFolders();
        }
    }
}