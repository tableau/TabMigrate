using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// A simple latch.  Stays false until its set to true
/// </summary>
class SimpleLatch
{
    public bool Value
    {
        get
        {
            return _latch;
        }
    }
    private bool _latch = false;

    public void Trigger()
    {
        _latch = true;
    }
}