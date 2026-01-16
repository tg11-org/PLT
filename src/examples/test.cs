using System;

class BatteryTest
{
    static void Main()
    {
        // Variables
        int batteryVoltage = 12;
        string batteryType = "Lithium";
        bool isCharged = true;
        
        // Basic output
        Console.WriteLine("Battery Test Started");
        Console.WriteLine(batteryType);
        
        // Conditionals
        if (batteryVoltage > 11)
        {
            Console.WriteLine("Voltage is good");
        }
        else
        {
            Console.WriteLine("Low voltage");
        }
        
        // Loop
        for (int i = 0; i < 3; i++)
        {
            Console.WriteLine("Cycle");
        }
        
        // Method call
        TestBattery();
    }
    
    static void TestBattery()
    {
        Console.WriteLine("Testing battery");
        int cycles = 5;
        while (cycles > 0)
        {
            Console.WriteLine("Running cycle");
            cycles = cycles - 1;
        }
    }
}
