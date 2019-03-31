using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Antidote
{
    public static class Common
    {

        public static void SetTaskManager(bool enable)
        {
            if (AppState.DEV_MODE)
            {
                return;
            }
            try
            {
                RegistryKey objRegistryKey = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System");
                if (enable && objRegistryKey.GetValue("DisableTaskMgr") != null)
                    objRegistryKey.DeleteValue("DisableTaskMgr");
                else
                    objRegistryKey.SetValue("DisableTaskMgr", "1");
                objRegistryKey.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
