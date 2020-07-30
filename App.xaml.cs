using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Sentry;
using Sentry.Protocol;

namespace KiriMusicHelper {
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application {
    }



    public class MainSentry
    {
        [System.STAThreadAttribute()]
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public static void Main()
        {
            using (SentrySdk.Init((o) => {
                o.Dsn = new Dsn("https://bb2850892c484c52a0fcf6a6e7672688@o251526.ingest.sentry.io/5371586");
                o.Release = "KiriMusicHelper@a1";
                o.Environment = "Alpha";
            }))
            {
                SentrySdk.ConfigureScope((scope) => {
                    scope.User = new User
                    {
                        Username = System.Security.Principal.WindowsIdentity.GetCurrent().Name
                    };
                });
                throw new Exception("test");
                App.Main();
            }

        }
    }


}
