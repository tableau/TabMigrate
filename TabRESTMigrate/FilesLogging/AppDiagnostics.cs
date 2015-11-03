using System;
using System.Collections.Generic;
using System.Text;

static class AppDiagnostics
{
    public static void Assert(bool condition, string text)
    {
        if (condition) return;

        System.Diagnostics.Debug.Assert(false, text);
    }
}
