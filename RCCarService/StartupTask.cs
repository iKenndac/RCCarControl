using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using System.Diagnostics;
using Windows.Devices.I2c;
using Windows.Devices.Enumeration;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace RCCarService
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;
        I2CUIDevice screen;
        MenuController menu;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            deferral = taskInstance.GetDeferral();
            ConnectToScreen(0x4a);
        }

        public async void ConnectToScreen(int deviceAddress)
        {

            // https://ms-iot.github.io/content/en-US/win10/samples/PinMappingsRPi2.htm
            // Get a selector string for bus "I2C1"
            Debug.WriteLine("Looking for I2C bus...");
            string aqs = I2cDevice.GetDeviceSelector();

            // Find the I2C bus controller with our selector string
            var dis = await DeviceInformation.FindAllAsync(aqs);
            if (dis.Count == 0)
            {
                Debug.WriteLine("No devices found.");
                return;
            }
            else
            {
                Debug.WriteLine("{0} device(s) found.", dis.Count);
            }

            // 0x40 is the I2C device address
            var settings = new I2cConnectionSettings(deviceAddress);
            settings.BusSpeed = I2cBusSpeed.StandardMode;

            // Create an I2cDevice with our selected bus controller and I2C settings
            I2cDevice device = await I2cDevice.FromIdAsync(dis[0].Id, settings);

            Debug.WriteLine("Got device {0}, {1}", device.DeviceId, device.ConnectionSettings.SlaveAddress);
            screen = new I2CUIDevice(device);

            // Menu
            
            MenuItem rootMenu = new MenuItem();
            MenuItem sensorMenu = new MenuItem("Sensors & Servos");
            rootMenu.AddChild(sensorMenu);

            MenuItem accelerometerMenuItem = new MenuItem("Accelerometer");
            accelerometerMenuItem.MenuItemChosen += delegate (MenuItem sender)
            {
                menu.PresentInfoScreen(new ConfirmationPromptInfoScreen("Not implemented!", ConfirmationPromptButtons.Tick));
                //menu.PresentInfoScreen(new AccelerometerInfoScreen(Car.Accelerometor));
            };
            sensorMenu.AddChild(accelerometerMenuItem);

            MenuItem distanceMenuItem = new MenuItem("Distances");
            distanceMenuItem.MenuItemChosen += delegate (MenuItem sender)
            {
                menu.PresentInfoScreen(new ConfirmationPromptInfoScreen("Not implemented!", ConfirmationPromptButtons.Tick));
                //menu.PresentInfoScreen(new DistancesInfoScreen(Car.FrontUltrasonicSensors, Car.RearUltrasonicSensor));
            };
            sensorMenu.AddChild(distanceMenuItem);

            MenuItem steeringMenuItem = new MenuItem("Steering Servo");
            steeringMenuItem.MenuItemChosen += delegate (MenuItem sender)
            {
                menu.PresentInfoScreen(new ConfirmationPromptInfoScreen("Not implemented!", ConfirmationPromptButtons.Tick));
                //menu.PresentInfoScreen(new ServoInfoScreen(Car.SteeringServo, "Steering"));
            };
            sensorMenu.AddChild(steeringMenuItem);

            MenuItem throttleMenuItem = new MenuItem("Throttle Servo");
            throttleMenuItem.MenuItemChosen += delegate (MenuItem sender)
            {
                menu.PresentInfoScreen(new ConfirmationPromptInfoScreen("Not implemented!", ConfirmationPromptButtons.Tick));
                //menu.PresentInfoScreen(new ServoInfoScreen(Car.ThrottleServo, "Throttle"));
            };
            sensorMenu.AddChild(throttleMenuItem);

            // --

            MenuItem exitMenuItem = new MenuItem("Shut down");
            exitMenuItem.MenuItemChosen += delegate (MenuItem sender)
            {
                ConfirmationPromptInfoScreen exitConfirmation = new ConfirmationPromptInfoScreen("Are you sure?");
                exitConfirmation.RespondToPrompt += delegate (ConfirmationPromptInfoScreen prompt, bool confirm)
                {
                    if (confirm)
                    {
                        screen.ClearScreen();
                        screen.WriteString("Goodbye!", 0, 4);
                        // TODO: Actually shutdown
                        return;
                    }
                };

                menu.PresentInfoScreen(exitConfirmation);
            };

            MenuItem restartProcessMenuItem = new MenuItem("Relaunch");
            restartProcessMenuItem.MenuItemChosen += delegate (MenuItem sender)
            {
                ConfirmationPromptInfoScreen restartConfirmation = new ConfirmationPromptInfoScreen("Are you sure?");
                restartConfirmation.RespondToPrompt += delegate (ConfirmationPromptInfoScreen prompt, bool confirm)
                {
                    if (confirm)
                    {
                        screen.ClearScreen();
                        // TODO: Actually relaunch
                        return;
                    }
                };

                menu.PresentInfoScreen(restartConfirmation);
            };

            MenuItem systemMenu = new MenuItem("System");
            systemMenu.AddChild(restartProcessMenuItem);
            systemMenu.AddChild(exitMenuItem);

            rootMenu.AddChild(systemMenu);

            menu = new MenuController(screen, rootMenu);
        }
    }
}