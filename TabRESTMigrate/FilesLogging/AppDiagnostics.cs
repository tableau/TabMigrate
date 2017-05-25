using System;
using System.Collections.Generic;
using System.Text;

static class AppDiagnostics
{
    public static void Assert(bool condition, string text)
    {
        if (condition) return;

//[2016-05-25] Asserting in a non UI thread appears to stall the work (probably a message box showing in an invisible context, thus no Abort/Retry/Ignore button to push)
//             It is better just to write the assert to the output.  For debugging, a breakpoint can be placed here.
//        System.Diagnostics.Debug.Assert(false, text);
        System.Diagnostics.Debug.WriteLine("ASSERT FAIL:" + text);
    }
}
