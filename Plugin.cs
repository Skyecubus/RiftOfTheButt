using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using Buttplug.Client;
using Buttplug.Core;
using HarmonyLib;
using RhythmRift;
using Shared.RiftInput;


namespace RiftOfTheButt;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("RiftOfTheNecroDancer.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);

    static PlugManager buttPlug;




    private async Task Setup()
    {
        
        
        var client = new ButtplugClient("RiftOfTheButt Client");
        var connector = new ButtplugWebsocketConnector(new System.Uri("ws://127.0.0.1:12345"));


        
        //try to connect to Intiface Central or other buttplug server
        try
        {
            await client.ConnectAsync(connector);
        }
        catch (ButtplugClientConnectorException ex)
        {
            Logger.LogInfo($"can't connect to Buttplug Server, exiting!" + $"Message: {ex.InnerException?.Message}");
            
        }
        catch (ButtplugHandshakeException ex)
        {
            Logger.LogInfo($"Handshake with buttplug server failed, exiting!" + $"Message: {ex.InnerException.Message}");
        }

        Logger.LogInfo($"Connected to Butt plug Server!");

        //scan for ButtPlug
        var startScanningTask = client.StartScanningAsync();
        try
        {
            await startScanningTask;
        }
        catch (ButtplugException ex)
        {
            Logger.LogInfo($"Scanning for device failed: {ex.InnerException.Message}");
        }

        Logger.LogInfo($"ButtPlug connected successfully!");

        //get device connected to client 
        var clientDevice = client.Devices[0];
        buttPlug = new PlugManager(clientDevice, clientDevice.DisplayName, clientDevice.VibrateAttributes.Count );

        

        
        

        

    }

        
    private async Task Awake()
    {
        harmony.PatchAll();

        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        await Setup();
    }

    




    //all of the patches for the RRStageController class in game 
    [HarmonyPatch(typeof(RRStageController))]
    internal class RRStageController_patch
    {


        [HarmonyPatch(typeof(RRStageController), "ActivateVibePower")]
        [HarmonyPostfix]
        public static async void ActivateVibePower_patch()
        {
            buttPlug.updateVals(0, 0.3);
            buttPlug.updateVibe();
        }

        [HarmonyPatch(typeof(RRStageController), "DeactivateVibePower")]
        [HarmonyPostfix]
        public static async void DeactivateVibePower_patch()
        {
            buttPlug.updateVals(0, 0);
            buttPlug.updateVibe();
            
        }

        [HarmonyPatch(typeof(RRStageController), "HandleEnemySlain")]
        [HarmonyPostfix]
        public static async void HandleEnemySlain_patch()
        {
            buttPlug.updateVals(buttPlug.mainRotorVal + 0.2, buttPlug.secondaryRotorVal);
            buttPlug.updateVibe();
            await Task.Delay(50);
            buttPlug.updateVals(buttPlug.mainRotorVal - 0.2, buttPlug.secondaryRotorVal);
            buttPlug.updateVibe();

        }
    
    }
    
    //[HarmonyPatch(typeof(VirtualKeyboardProvider))]
    //internal class Update_patch
    //{
    //    [HarmonyPatch(typeof(VirtualKeyboardProvider), "Update")]
    //    [HarmonyPostfix]
    //    public static async void update_patched()
    //    {
            
    //    }
        
    //}


   
}

class PlugManager
{
    public ButtplugClientDevice device {get;}
    public String deviceName {get;}
    public bool deviceConnected {get; set;}
    public int numberOfRotors {get;}
    public double mainRotorVal {get; set;}
    public double secondaryRotorVal {get; set;}

    public PlugManager(ButtplugClientDevice device, String deviceName, int numberOfRotors)
    {
        this.device = device;
        this.deviceName = deviceName;
        this.numberOfRotors = numberOfRotors;
        this.deviceConnected = true;
        this.mainRotorVal = 0.0;
        if (numberOfRotors > 1)
        {
            this.secondaryRotorVal = 0.0;
        }    
    }

    public async void updateVals(double mainRotorVal, double secondaryRotorVal = 0.0)
    {

        this.mainRotorVal = mainRotorVal;

        if (this.numberOfRotors > 1)
        {
            this.secondaryRotorVal = secondaryRotorVal;
        }


    }

    public async void updateVibe()
    {

       if (this.numberOfRotors == 1)
        {
            if (secondaryRotorVal > mainRotorVal){
                await this.device.VibrateAsync(this.secondaryRotorVal);
            }
            else
            {
                await this.device.VibrateAsync(this.mainRotorVal);  
            }
            
        }
        else
        {
            await this.device.VibrateAsync(new[]{this.mainRotorVal, this.secondaryRotorVal});
        }
        
    }






}




