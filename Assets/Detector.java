package com.evgenindev.simdetector;

import android.app.Activity;
import android.content.Context;
import android.telephony.TelephonyManager;
import android.util.Log;

public class Detector{
    private static final Detector Instance = new Detector();
    private static final String LOGTAG = "EvGenInDev";
    public static Detector getInstance() { return Instance; }

    private long startTime;
    private Detector(){
        Log.i(LOGTAG, "Created plugin");
    }

    public int getSimStatus(Activity act)
    {
        TelephonyManager TM = (TelephonyManager) act.getSystemService(Context.TELEPHONY_SERVICE);
        return !(TM.getSimState() == TelephonyManager.SIM_STATE_ABSENT) ? 1 : 0;
    }
}
