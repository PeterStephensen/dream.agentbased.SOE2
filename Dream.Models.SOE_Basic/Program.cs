﻿using System;
using System.Runtime;
using System.Text.Json;
using System.IO;

namespace Dream.Models.SOE_Basic
{
    class Program
    {
        static void Main(string[] args)
        {
            RunSimulation(args, false); // Mark saveScenario here!!           
        }   
    
        static void RunSimulation(string[] args, bool saveScenario=false)
        {
            Settings settings = new();
            settings.SaveScenario = saveScenario;
            
            // Scale
            double scale = 5 * 1.0; 
            
            settings.NumberOfSectors = 1;
            settings.NumberOfFirms = (int)(150 * scale);
            settings.NumberOfHouseholdsPerFirm = 5;
            settings.HouseholdNewBorn = (int)(5 * scale);   //15
            settings.InvestorInitialInflow = (int)(10 * scale);
            settings.HouseholdNumberConsumptionPerPeriod = 4; // Weekly consumption

            //Firms
            settings.FirmParetoMinPhi = 0.5;
            settings.FirmPareto_k = 2.5;  // k * (1 - alpha) > 1     

            settings.FirmParetoMinPhiInitial = 1.9;

            settings.FirmAlpha = 0.5;
            settings.FirmFi = 2;

            //-----
            //double mark = 0.08; // SE HER !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //ouble sens = 0.5;
            double mark = 0.15; // SE HER !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            double sens = 1/0.05;

            // Wage ----------------------------------
            settings.FirmWageMarkup = 1 * mark;                                            //2  !!!!
            settings.FirmWageMarkupSensitivity = 0.5 * sens;
            settings.FirmWageMarkdown = 1 * mark;    // 0 is ok
            settings.FirmWageMarkdownSensitivity = 0.5 * sens;

            // In zone
            settings.FirmWageMarkupInZone = 1 * mark;                                      //2  !!!!
            settings.FirmWageMarkupSensitivityInZone = 0.5 * sens;
            settings.FirmWageMarkdownInZone = 1 * mark;    // 0 is ok
            settings.FirmWageMarkdownSensitivityInZone = 0.5 * sens;

            settings.FirmProbabilityRecalculateWage = 0.5;
            settings.FirmProbabilityRecalculateWageInZone = 0.5; // 0.2 

            // Price ----------------------------------
            settings.FirmPriceMarkup = 1 * mark;
            settings.FirmPriceMarkupSensitivity = sens;
            settings.FirmPriceMarkdown = 1 * mark;             //Stable prices  
            settings.FirmPriceMarkdownSensitivity = sens;  //Stable prices 

            // In zone
            settings.FirmPriceMarkupInZone = 1 * mark;
            settings.FirmPriceMarkupSensitivityInZone = sens;
            settings.FirmPriceMarkdownInZone = 1 * mark;                //Stable prices  
            settings.FirmPriceMarkdownSensitivityInZone = sens;    //Stable prices 

            settings.FirmProbabilityRecalculatePrice = 0.5;
            settings.FirmProbabilityRecalculatePriceInZone = 0.5; // 0.2

            settings.FirmPriceMechanismStart = 12 * 1;

            //-----
            settings.FirmComfortZoneEmployment = 0.15;
            settings.FirmComfortZoneSales = 0.15;

            //-----
            settings.FirmDefaultProbabilityNegativeProfit = 0.5;  
            settings.FirmDefaultStart = 12 * 5;
            settings.FirmNegativeProfitOkAge = 12 * 2;

            settings.FirmExpectationSmooth = 0.4;
            settings.FirmMaxEmployment = 1000;  // 700

            settings.FirmVacanciesShare = 1.0;
            settings.FirmMinRemainingVacancies = 5;

            settings.FirmProfitLimitZeroPeriod = (2040 - 2014) * 12;

            settings.FirmProductivityGrowth = 0.02;

            // Households
            settings.HouseholdNumberFirmsSearchJob = 15;     // 15  
            settings.HouseholdNumberFirmsSearchShop = 75;    // 15 
            settings.HouseholdProbabilityQuitJob = 0.01;
            settings.HouseholdProbabilitySearchForJob = 0.25;                        
            settings.HouseholdProbabilitySearchForShop = 0.25;                          // MEGET LAV???!!!
            settings.HouseholdProductivityLogSigmaInitial = 0.6;
            settings.HouseholdProductivityLogMeanInitial = -0.5 * Math.Pow(settings.HouseholdProductivityLogSigmaInitial, 2); // Sikrer at forventet produktivitet er 1
            settings.HouseholdProductivityErrorSigma = 0.02;
            settings.HouseholdCES_Elasticity = 0.7;
            settings.HouseholdDisSaveRatePensioner = 0.01;
            settings.HouseholdDisSaveRateUnemployed = 0.05;
            settings.HouseholdSaveRate = 0.01;
            settings.NumberOfInheritors = 5;
            settings.HouseholdMaxNumberShops = 15; // 5 When your supplier can not deliver: how many to seach for

            settings.HouseholdPensionAge = 67 * 12;
            settings.HouseholdStartAge = 18 * 12;

            // Investor
            settings.InvestorProfitSensitivity = 0.15;   // 0.15            

            // Statistics
            settings.StatisticsInitialMarketPrice = 1.2;  //2.0
            settings.StatisticsInitialMarketWage = 0.2;   //1.0 
            settings.StatisticsInitialInterestRate = Math.Pow(1 + 0.05, 1.0 / 12) - 1; // 5% p.a.

            settings.StatisticsFirmReportSampleSize = 0.05;
            settings.StatisticsHouseholdReportSampleSize = 0.001;

            settings.StatisticsExpectedSharpeRatioSmooth = 0.7;

            // R-stuff
            if (Environment.MachineName == "C1709161") // PSP's gamle maskine
            {
                settings.ROutputDir = @"C:\test\Dream.AgentBased.MacroModel";
                settings.RExe = @"C:\Program Files\R\R-4.0.3\bin\x64\R.exe";
            }
            if (Environment.MachineName == "C2210098") // PSP's nye maskine
            {
                settings.ROutputDir = @"C:\Users\B007566\Documents\Output";
                settings.RExe = @"C:\Program Files\R\R-4.2.0\bin\x64\R.exe";
            }

            if (Environment.MachineName == "VDI00316") // Fjernskrivebord
            {
                settings.ROutputDir = @"C:\Users\B007566\Documents\Output";
                settings.RExe = @"C:\Users\B007566\Documents\R\R-4.1.2\bin\x64\R.exe";
            }

            if (Environment.MachineName == "VDI00382") // Fjernskrivebord til Agentbased projekt
            {
                settings.ROutputDir = @"C:\Users\B007566\Documents\Output";
                settings.RExe = @"C:\Users\B007566\Documents\R\R-4.1.3\bin\x64\R.exe";
            }

            // Time and randomseed           
            settings.StartYear = 2014;
            settings.EndYear = 2215;
            //settings.EndYear = 2050;   //**************************************   
            settings.PeriodsPerYear = 12;

            settings.StatisticsOutputPeriode = (2075 - 2014) * 12;
            settings.StatisticsGraphicsPlotInterval = 12 * 1;
            
            settings.StatisticsGraphicsStartPeriod = (2080 - 2014) * 12;   // 70
            if(settings.SaveScenario)
                settings.StatisticsGraphicsStartPeriod = 12 * 500;


            //settings.RandomSeed = 123;  
            //settings.FirmNumberOfNewFirms = 1;

            settings.BurnInPeriod1 = (2030 - 2014) * 12;  //35
            settings.BurnInPeriod2 = (2035 - 2014) * 12;  //50
            settings.StatisticsWritePeriode = (2075 - 2014) * 12;

            // !!!!! Remember some settings are changed in Simulation after BurnIn1 !!!!!
            //settings.BurnInPeriod1 = 1;
            ////settings.BurnInPeriod2 = 112 * 5;
            //settings.FirmProfitLimitZeroPeriod = 1;
            //settings.FirmDefaultStart = 1;
            //settings.LoadDatabase = true;

            settings.RandomParameters=true;
            if(settings.RandomParameters)
            {

                if (args.Length != 1)   // Base-run
                {
                    Random rnd = new();
                    settings.InvestorProfitSensitivity = rnd.NextDouble(0.2, 0.8);

                    double m = rnd.NextDouble(0.05, 0.25);
                    double s = rnd.NextDouble(5.0, 20);
                    settings.FirmPriceMarkup = m;
                    settings.FirmPriceMarkupInZone = m;
                    settings.FirmPriceMarkupSensitivity = s;
                    settings.FirmPriceMarkupSensitivityInZone = s;

                    m = rnd.NextDouble(0.05, 0.25);
                    s = rnd.NextDouble(5.0, 20);
                    settings.FirmPriceMarkdown = m;
                    settings.FirmPriceMarkdownInZone = m;
                    settings.FirmPriceMarkdownSensitivity = s;
                    settings.FirmPriceMarkdownSensitivityInZone = s;

                    m = rnd.NextDouble(0.05, 0.25);
                    s = rnd.NextDouble(10, 30);
                    settings.FirmWageMarkup = m;
                    settings.FirmWageMarkupInZone = m;
                    settings.FirmWageMarkupSensitivity = s;
                    settings.FirmWageMarkupSensitivityInZone = s;

                    m = rnd.NextDouble(0.05, 0.25);
                    s = rnd.NextDouble(10, 30);
                    settings.FirmWageMarkdown = m;
                    settings.FirmWageMarkdownInZone = m;
                    settings.FirmWageMarkdownSensitivity = s;
                    settings.FirmWageMarkdownSensitivityInZone = s;

                }
                else   // Counterfactuals
                {
                    settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settings.ROutputDir + "\\last_json.json"));
                }
            }
            
            if (args.Length == 1)
            {
                //settings.Shock = EShock.Tsunami;
                //settings.IDScenario = Int32.Parse(args[0]);
                settings.Shock = (EShock)Int32.Parse(args[0]);
                settings.ShockPeriod = (2105 - 2014) * 12;
            }

            settings.NewScenarioDirs = false;
            var t0 = DateTime.Now;

            // Run the simulation
            new Simulation(settings, new Time(0, (1 + settings.EndYear - settings.StartYear) * settings.PeriodsPerYear - 1));

            Console.Write("\n");
            Console.WriteLine(DateTime.Now - t0);
            //Console.ReadLine();

        }

    }
}
