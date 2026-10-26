using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using MemePets;
class IntegrationHost {
    [STAThread] static int Main(string[] args) {
        Role role=Program.GetRole(args);bool fresh;
        using(var mutex=new Mutex(true,Native.MutexName(role),out fresh)) {
            if(!fresh)return 2;
            var app=new Application {ShutdownMode=ShutdownMode.OnMainWindowClose};
            var window=new PetWindow(role,args) {ShowInTaskbar=true};
            window.SourceInitialized+=(s,e)=>{
                var hwnd=new WindowInteropHelper(window).Handle;
                Native.SetWindowLong(hwnd,-20,Native.GetWindowLong(hwnd,-20)&~0x80);
            };
            app.Run(window);GC.KeepAlive(mutex);
        }
        return 0;
    }
}
