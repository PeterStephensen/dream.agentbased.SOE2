﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Diagnostics;
//using System.Threading;
using System.Text.Json;


using Dream.AgentClass;
using Dream.IO;

namespace Dream.Models.SOE_Basic
{

    #region FirmInfo Class
    /// <summary>
    /// This class contains information to the Statistics object
    /// </summary>
    public class FirmInfo
    {
        public int Age { get; set; }
        public int Sector { get; set; }
        public double Profit { get; set; }

        public FirmInfo(Firm firm)
        {
            Age = firm.Age;
            Sector = firm.Sector;
            Profit = firm.Profit;
        }
    }
    #endregion

    public class Statistics : Agent
    {

        #region Private fields
        Simulation _simulation;
        Settings _settings;
        Time _time;
        double[] _marketPrice, _marketWage;
        double[] _employment, _sales, _production;
        double _marketWageTotal = 0;
        double _marketPriceTotal = 0;
        double _profitPerHousehold, _expProfit;
        double _avrProductivity = 0;
        StreamWriter _fileFirmReport;
        StreamWriter _fileHouseholdReport;
        StreamWriter _fileDBHouseholds;
        StreamWriter _fileDBFirms;
        StreamWriter _fileDBStatistics;
        StreamWriter _fileMacro;
        StreamWriter _fileSectors;
        double _macroProductivity = 1.0;
        double[] _sectorProductivity;
        double _interestRate;
        double _meanValue = 0;
        double _discountedProfits = 0;
        int _nFirmCloseNatural = 0, _nFirmCloseNegativeProfit = 0, _nFirmCloseTooBig = 0, _nFirmCloseZeroEmployment=0;
        double _nFirmNew = 0;
        double _expDiscountedProfits = 0;
        double _sharpeRatioTotal = 0;
        double _sigmaRiskTotal = 0;
        double _expSharpeRatioTotal = 0;
        double[] _sharpeRatio;
        double[] _sigmaRisk;
        double[] _expSharpeRatio;
        double _yr_consumption = 0; 
        int _yr_employment = 0;
        double _totalEmployment = 0;
        double _totalSales = 0;
        double _totalPotensialSales = 0;
        double _totalProduction = 0;
        int _scenario_id = 0;
        string _runName = "Base";
        double _laborSupply = 0;  // Measured in productivity units
        int _n_laborSupply = 0;   // Measured in heads
        int _n_unemployed = 0;     // Measured in heads
        int _n_couldNotFindSupplier=0;
        List<FirmInfo> _firmInfo = new List<FirmInfo>();
        int _nChangeShopInSearchForShop = 0;
        int _nChangeShopInBuyFromShopNull = 0;
        int _nChangeShopInBuyFromShopLookingForGoods = 0;
        int _nCouldNotFindFirmWithGoods = 0;
        int _nBuyFromShop = 0;
        int _nSuccesfullTrade = 0;
        int _nZeroBudget = 0;
        int _nSuccesfullTradeNonZero = 0;
        #endregion

        #region Constructor
        public Statistics()
        {
            _simulation = Simulation.Instance;
            _settings = _simulation.Settings;
            _time = _simulation.Time;

            _marketPrice = new double[_settings.NumberOfSectors];
            _marketWage = new double[_settings.NumberOfSectors];
            _employment = new double[_settings.NumberOfSectors];
            _sales = new double[_settings.NumberOfSectors];
            _production = new double[_settings.NumberOfSectors];
            _sectorProductivity = new double[_settings.NumberOfSectors];
            _sigmaRisk = new double[_settings.NumberOfSectors];
            _sharpeRatio = new double[_settings.NumberOfSectors];
            _expSharpeRatio = new double[_settings.NumberOfSectors];

            for (int i = 0; i < _settings.NumberOfSectors; i++)
            {
                _marketPrice[i] = _settings.StatisticsInitialMarketPrice;
                _marketWage[i] = _settings.StatisticsInitialMarketWage;
                _sectorProductivity[i] = 1.0;
            }
            _marketPriceTotal = _settings.StatisticsInitialMarketPrice;
            _marketWageTotal = _settings.StatisticsInitialMarketWage;
            _interestRate = _settings.StatisticsInitialInterestRate;

            //var options = new JsonSerializerOptions { WriteIndented = true };
            //string sJson = JsonSerializer.Serialize(_settings, options);
            //File.WriteAllText(_settings.ROutputDir + "\\Settings.json", sJson);

            if (_settings.LoadDatabase)
            {
                TabFileReader file = new TabFileReader(_settings.ROutputDir + "\\db_statistics.txt");
                file.ReadLine();

                //_marketPrice = file.GetDouble("marketPrice");
                //_marketWage = file.GetDouble("marketWage");
                _expSharpeRatioTotal = file.GetDouble("expSharpeRatio");
                _sharpeRatioTotal = file.GetDouble("expSharpeRatio");

                file.Close();

            }

        }
        #endregion

        #region EventProc
        public override void EventProc(int idEvent)
        {
            
            switch (idEvent)
            {

                case Event.System.Start:
                    OpenFiles();
                    //File.Copy("..\\..\\..\\R\\graphs.R", _settings.ROutputDir + "\\graphs.R", true);
                    break;

                case Event.System.PeriodStart:
                    #region Event.System.PeriodStart

                    if (_time.Now == _settings.StatisticsWritePeriode)
                    {

                        string path = _settings.ROutputDir + "\\db_households.txt";
                        if (File.Exists(path)) File.Delete(path);
                        _fileDBHouseholds = File.CreateText(path);
                        _fileDBHouseholds.WriteLine("ID\tAge\tFirmEmploymentID\tFirmShopID\tProductivity");

                        path = _settings.ROutputDir + "\\db_firms.txt";
                        if (File.Exists(path)) File.Delete(path);
                        _fileDBFirms = File.CreateText(path);
                        _fileDBFirms.WriteLine("ID\tAge\tphi0\texpPrice\texpWage\texpQuitters\texpApplications\texpPotentialSales\texpSales\tw\tp\tSales\tProfit");

                        path = _settings.ROutputDir + "\\db_statistics.txt";
                        if (File.Exists(path)) File.Delete(path);
                        _fileDBStatistics = File.CreateText(path);
                        _fileDBStatistics.WriteLine("expSharpeRatio\tmacroProductivity\tmarketPrice\tmarketWage");

                    }

                    if (_time.Now == _settings.StatisticsWritePeriode + 1)
                    {
                        if (_fileDBHouseholds != null)
                            _fileDBHouseholds.Close();

                        if (_fileDBFirms != null)
                            _fileDBFirms.Close();

                        if (_fileDBStatistics != null)
                            _fileDBStatistics.Close();
                    }

                    //Calculate Profit Per Household and Sharpe Ratios
                    _profitPerHousehold = 0;
                    double discountedProfitsTotal = 0;
                    double[] discountedProfits = new double[_settings.NumberOfSectors];
                    int[] nFirms = new int[_settings.NumberOfSectors];
                    foreach (var fi in _firmInfo)
                    {
                        _profitPerHousehold += fi.Profit;

                        discountedProfitsTotal += fi.Profit / Math.Pow(1 + _interestRate, fi.Age); 
                        discountedProfits[fi.Sector] += fi.Profit / Math.Pow(1 + _interestRate, fi.Age);
                        nFirms[fi.Sector]++; 
                    }

                    _profitPerHousehold /= _simulation.Households.Count;
                    
                    double dpTotal = discountedProfitsTotal / _firmInfo.Count;
                    
                    double[] dp = new double[_settings.NumberOfSectors];
                    for (int i = 0; i < _settings.NumberOfSectors; i++) dp[i] = discountedProfits[i] / nFirms[i];

                    _sigmaRiskTotal = 0;
                    for (int i = 0; i < _settings.NumberOfSectors; i++) _sigmaRisk[i] = 0;
                    foreach (var fi in _firmInfo)
                    {
                        _sigmaRiskTotal += Math.Pow(fi.Profit / Math.Pow(1 + _interestRate, fi.Age) - dpTotal, 2);
                        _sigmaRisk[fi.Sector] += Math.Pow(fi.Profit / Math.Pow(1 + _interestRate, fi.Age) - dp[fi.Sector], 2);
                    }

                    _sigmaRiskTotal = Math.Sqrt(_sigmaRiskTotal / _firmInfo.Count);
                    for (int i = 0; i < _settings.NumberOfSectors; i++) _sigmaRisk[i] = Math.Sqrt(_sigmaRisk[i] / nFirms[i]);

                    _sharpeRatioTotal = _sigmaRiskTotal > 0 ? dpTotal / _sigmaRiskTotal : 0;
                    _expSharpeRatioTotal = _settings.StatisticsExpectedSharpeRatioSmooth * _expSharpeRatioTotal + (1 - _settings.StatisticsExpectedSharpeRatioSmooth) * _sharpeRatioTotal;

                    for (int i = 0; i < _settings.NumberOfSectors; i++)
                    {
                        _sharpeRatio[i] = _sigmaRisk[i] > 0 ? dp[i] / _sigmaRisk[i] : 0;
                        _expSharpeRatio[i] = _settings.StatisticsExpectedSharpeRatioSmooth * _expSharpeRatio[i] + (1 - _settings.StatisticsExpectedSharpeRatioSmooth) * _sharpeRatio[i];
                    }

                    //int z = 0;
                    //if (_time.Now > 12 * 30)
                    //    z++;

                    //_totalProfitFromDefaults = 0;
                    //_profitPerHousehold = 0;
                    _n_couldNotFindSupplier = 0;
                    _firmInfo = new List<FirmInfo>();

                    //_totalProfit = 0;

                    //_nFirmCloseNatural = 0;
                    //_nFirmCloseTooBig = 0;
                    //_nFirmCloseNegativeProfit = 0;
                    //_nFirmCloseZeroEmployment = 0;
                    //_nFirmNew = 0;
                    _nChangeShopInSearchForShop = 0;
                    _nChangeShopInBuyFromShopNull = 0;
                    _nChangeShopInBuyFromShopLookingForGoods = 0;
                    _nCouldNotFindFirmWithGoods = 0;
                    _nBuyFromShop = 0;
                    _nSuccesfullTrade = 0;
                    _nZeroBudget = 0;
                    _nSuccesfullTradeNonZero = 0;

                    break;
                    #endregion

                case Event.System.PeriodEnd:
                    #region Event.System.PeriodEnd
                    if (_time.Now == _settings.StatisticsWritePeriode)
                        Write();

                    // Profit income to households. What comes from defaults and deaths during Update
                    //_profitPerHousehold += _totalProfitFromDefaults / _simulation.Households.Count;  

                    // TWO LOOPS over firms !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    double totalRevenues = 0;
                    for (int i = 0; i < _settings.NumberOfSectors; i++)
                    {
                        double meanWage = 0;
                        double meanPrice = 0;
                        //double discountedProfits = 0;
                        _employment[i] = 0;
                        _sales[i] = 0;

                        foreach (Firm f in _simulation.Sector(i))
                        {
                            meanWage += f.Wage * f.Employment;
                            meanPrice += f.Price * f.Sales;
                            _employment[i] += f.Employment;
                            _sales[i] += f.Sales;
                            totalRevenues += f.Price * f.Sales;
                            _production[i] += f.Production;
                            //discountedProfits += f.Profit / Math.Pow(1 + _interestRate, f.Age);
                        }
                        
                        if (meanWage > 0)
                            _marketWage[i] = meanWage / _employment[i];

                        if (meanPrice > 0 & _sales[i] > 0)
                            _marketPrice[i] = meanPrice / _sales[i];
                    }
                    
                    _meanValue = 0;
                    _discountedProfits = 0;
                    _totalSales = 0;
                    _totalPotensialSales = 0;
                    _totalEmployment = 0;
                    _totalProduction = 0;
                    //_totalProfit = _totalProfitFromDefaults; 
                    //double totProfit = 0;
                    double mean_age = 0;
                    double tot_vacancies = 0;
                    double meanWageTot = 0;
                    double meanPriceTot = 0;
                    double nEmployment = 0;
                    //double potentialSales = 0;
                    int no = 0;
                    int ok = 0;
                    for (int i = 0; i < _settings.NumberOfSectors; i++)
                        foreach (Firm f in _simulation.Sector(i))
                        {
                            meanWageTot += f.Wage * f.Employment;
                            meanPriceTot += f.Price * f.Sales;
                            _totalEmployment += f.Employment;
                            nEmployment += f.NumberOfEmployed; 
                            _totalSales += f.Sales;                              // Hmm..
                            _totalPotensialSales += f.PotentialSales;            // Hmm..
                            _totalProduction += f.Production;                    // Hmm..
                            _meanValue += f.Value;
                            mean_age += f.Age;
                            tot_vacancies += f.Vacancies;
                            _discountedProfits += f.Profit / Math.Pow(1+_interestRate, f.Age);
                            no += f.NumberOfNo;
                            ok += f.NumberOfOK;
                        }
                   
                    _avrProductivity = _totalEmployment / nEmployment;
                    //double firmRejectionRate = (double)no / (no + ok);
                    //double potentilaSalesRate = _totalPotensialSales / _totalSales;
                    // Calculation of profitPerHousehold
                    //_profitPerHousehold = _totalProfit / _simulation.Households.Count;  // Profit income to households
                    //_totalProfitFromDefaults = 0;

                    int n_firms = 0;
                    for (int i = 0; i < _settings.NumberOfSectors; i++)
                        n_firms += _simulation.Sector(i).Count;

                    //double m_pi = _discountedProfits /n_firms;
                    //_sigmaRiskTotal = 0;

                    //for (int i = 0; i < _settings.NumberOfSectors; i++)
                    //    foreach (Firm f in _simulation.Sector(i))
                    //        _sigmaRiskTotal += Math.Pow(f.Profit / Math.Pow(1 + _interestRate, f.Age) - m_pi, 2);
                    //_sigmaRiskTotal = Math.Sqrt(_sigmaRiskTotal / n_firms);
                    //_sharpeRatioTotal = _sigmaRiskTotal > 0 ? m_pi / _sigmaRiskTotal : 0;

                    _expDiscountedProfits = 0.99 * _expDiscountedProfits + (1 - 0.99) * _discountedProfits; // Bruges ikke
                    //_expSharpeRatioTotal = _settings.StatisticsExpectedSharpeRatioSmooth * _expSharpeRatioTotal + (1 - _settings.StatisticsExpectedSharpeRatioSmooth) * _sharpeRatioTotal;
                    mean_age /= n_firms;
                    _meanValue /= n_firms;
                    //_expProfit = totProfit / n_firms;

                    if (meanWageTot > 0)
                        _marketWageTotal = meanWageTot / _totalEmployment;

                    //----------
                    int nUnemp = 0;
                    int laborSupply = 0;
                    _n_laborSupply = 0;
                    _laborSupply = 0;
                    _n_unemployed = 0;
                    double totalConsumption = 0;
                    int h_no = 0;
                    int h_ok = 0;
                    double consValue = 0, consBudget = 0;
                    foreach (Household h in _simulation.Households)
                    {
                        if (h.Age < _settings.HouseholdPensionAge)
                        {
                            nUnemp += h.Unemployed ? 1 : 0;
                            laborSupply++;
                            _n_laborSupply++;
                            _laborSupply += h.Productivity;
                            _n_unemployed += h.Unemployed ? 1 : 0;
                        }
                        h_no += h.No;
                        h_ok += h.Ok;
                        consValue += h.ConsumptionValue;
                        consBudget += h.ConsumptionBudget;
                        totalConsumption += h.Consumption;
                    }
                    //double h_rejectionRate = (double)h_no / (h_no + h_ok);
                    double consLoss = 1.0 - consValue / consBudget;
                    // Calculate median wage
                    //if (wages.Count > 0)
                    //    _marketWage = wages.Median();

                    if (_time.Now > _settings.FirmPriceMechanismStart)
                    {
                        // Calculate median price
                        //if (prices.Count > 0)
                        //    _marketPrice = prices.Median();

                        if (meanPriceTot > 0 & _totalSales > 0)
                            _marketPriceTotal = meanPriceTot / _totalSales;

                        //_discountedProfits /= _marketPrice;
                    }

                    if((_time.Now + 1) % _settings.PeriodsPerYear == 0)
                    {
                        // REMOVE THIS!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                        _yr_consumption = 0;
                        _yr_employment = 0;
                        foreach (Household h in _simulation.Households)
                        {
                            _yr_consumption += h.YearConsumption;
                            _yr_employment += h.YearEmployment;
                        }
                    }

                    #region Graphics
                    // Graphics
                    double[] price = new double[_settings.NumberOfSectors];
                    double[] wage = new double[_settings.NumberOfSectors];
                    double[] employment = new double[_settings.NumberOfSectors];
                    double[] production = new double[_settings.NumberOfSectors];
                    //double[] sales = new double[_settings.NumberOfSectors];
                    //double[] potensialSales = new double[_settings.NumberOfSectors];
                    int[] nFirm = new int[_settings.NumberOfSectors];

                    if (_settings.StatisticsGraphicsPlotInterval > 0 & (_time.Now > _settings.StatisticsGraphicsStartPeriod))
                        if (_time.Now % _settings.StatisticsGraphicsPlotInterval == 0) // Once a year
                        {
                            double tot_opt_l = 0;// Calculate total optimal employment  
                            double prod_avr = 0; // Calculate average productivity
                            using (StreamWriter sw = File.CreateText(_settings.ROutputDir + "\\data_firms.txt"))
                            {

                                sw.WriteLine("Productivity\tOptimalEmployment\tOptimalProduction\tEmployment\tProfit\tSales\tAge\tOptimalProfit\t" +
                                    "NumberOfEmployed\tVacancies\tApplications\tQuitters\tSector");
                                for (int i = 0; i < _settings.NumberOfSectors; i++)
                                {
                                    price[i] = 0;
                                    wage[i] = 0;
                                    employment[i] = 0;
                                    production[i] = 0;
                                    //sales[i] = 0;
                                    //potensialSales[i] = 0;
                                    nFirm[i] = _simulation.Sector(i).Count;
                                    foreach (Firm f in _simulation.Sector(i))
                                    {
                                        double disc = 1.0 / (1 + _interestRate);
                                        double discProfit = f.Profit * Math.Pow(disc, f.Age) / _marketPrice[f.Sector];

                                        sw.WriteLineTab(f.Productivity, f.OptimalEmployment, f.OptimalProduction, f.Employment, f.Profit,
                                            f.Sales, f.Age, f.OptimalProduction, f.NumberOfEmployed, f.Vacancies, f.JobApplications, f.JobQuitters,
                                            f.Sector);

                                        prod_avr += Math.Pow(f.Productivity, 1 / (1 - _settings.FirmAlpha));
                                        tot_opt_l += f.OptimalEmployment;

                                        price[i] += f.Price;
                                        wage[i] += f.Wage;
                                        employment[i] += f.Employment;
                                        production[i] += f.Production;
                                        //sales[i] += f.Sales;
                                        //potensialSales[i] += f.PotentialSales;
                                    }
                                    price[i] /= nFirm[i];
                                    wage[i] /= nFirm[i];
                                }
                            }

                            //sw.WriteLine("Time\tSector\tPrice\tWage\tEmployment\tProduction\tSales\tExpShapeRatio\tnFirm");

                            using (StreamWriter sw = File.AppendText(_settings.ROutputDir + "\\sector_year.txt"))
                            {
                                for (int i = 0; i < _settings.NumberOfSectors; i++)
                                    sw.WriteLineTab(_time.Now, i, price[i], wage[i], employment[i], production[i], 
                                        _sales[i], nFirm[i], _expSharpeRatio[i], _expSharpeRatioTotal);
                                sw.Flush();
                            }

                            prod_avr /= n_firms;
                            prod_avr = Math.Pow(prod_avr, 1 - _settings.FirmAlpha);
                            double P_star = 0;  // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                            //sw.WriteLine("Year\tn_Households\tavr_productivity\tnUnemployed\tnOptimalEmplotment\tP_star\tnEmployment\tnVacancies\tWage\tPrice\t" +
                            //"Sales\tProfitPerHousehold\tnFirms\tProfitPerFirm\tMeanAge\tMeanValue\tnFirmCloseNatural\tnFirmCloseNegativeProfit\tnFirmCloseTooBig\t" +
                            //"nFirmNew\tDiscountedProfits\tExpDiscountedProfits\tSharpeRatio\tExpSharpRatio\tLaborSupply\tYearConsumption\tYearEmployment");

                            using (StreamWriter sw = File.AppendText(_settings.ROutputDir + "\\data_year.txt"))
                            {

                                
                                sw.WriteLineTab(1.0 * _settings.StartYear + 1.0 * _time.Now / _settings.PeriodsPerYear, _time.Now,
                                    _simulation.Households.Count, prod_avr, nUnemp, tot_opt_l, P_star, _totalEmployment, 
                                    tot_vacancies, _marketWageTotal, _marketPriceTotal, _totalSales, _profitPerHousehold,
                                    n_firms, _expProfit, mean_age, _meanValue, _nFirmCloseNatural, 
                                    _nFirmCloseZeroEmployment, _nFirmCloseTooBig, _nFirmNew, _discountedProfits, 
                                    _expDiscountedProfits, _sharpeRatioTotal, _expSharpeRatioTotal, laborSupply, _yr_consumption, _yr_employment, 
                                    _totalPotensialSales, _totalProduction, totalConsumption, consValue, consBudget,
                                    _nChangeShopInSearchForShop, _nChangeShopInBuyFromShopNull, _nChangeShopInBuyFromShopLookingForGoods, 
                                    _nCouldNotFindFirmWithGoods, _nBuyFromShop, _nSuccesfullTrade, _nZeroBudget, _nSuccesfullTradeNonZero);
                                sw.Flush();

                            }

                            using (StreamWriter sw = File.CreateText(_settings.ROutputDir + "\\data_households.txt"))
                            {
                                sw.WriteLine("UnemplDuration\tProductivity\tAge\tConsumptionValue\tConsumptionBudget\tPrice\tWage\tIncome");
                                foreach (Household h in _simulation.Households)
                                {
                                    double w = h.FirmEmployment!=null ? h.FirmEmployment.Wage : 0;
                                    if(_simulation.Random.NextEvent(0.1))
                                        sw.WriteLineTab(h.UnemploymentDuration, h.Productivity, h.Age, h.ConsumptionValue, h.ConsumptionBudget, h.CES_Price, w, h.Income);
                                }
                            }

                            //RunRScript("..\\..\\..\\R\\graphs.R");
                            //Console.WriteLine("Running R..");
                            RunRScript(_settings.ROutputDir + "\\graphs.R");

                        }
                    #endregion

                    // Shock: Productivity shock
                    if (_time.Now == _settings.ShockPeriod)
                    {
                        if(_settings.Shock==EShock.Productivity)
                            _macroProductivity = 1.1;

                        if (_settings.Shock == EShock.ProductivitySector0)
                            _sectorProductivity[0] = 1.1;

                    }

                    int nFirmClosed = _nFirmCloseNatural + _nFirmCloseNegativeProfit + _nFirmCloseTooBig + _nFirmCloseZeroEmployment;
                    _fileMacro.WriteLineTab(_scenario_id, Environment.MachineName, _runName, _time.Now, _expSharpeRatioTotal, _macroProductivity, _marketPriceTotal, _marketWageTotal,
                                                n_firms, _totalEmployment, _totalSales, _laborSupply, _n_laborSupply, _n_unemployed,
                                                _totalProduction, _simulation.Households.Count, _nFirmNew, nFirmClosed, _sigmaRiskTotal, _sharpeRatioTotal, 
                                                mean_age, tot_vacancies, _marketPrice[0], _marketWage[0], _employment[0], _sales[0], 
                                                _simulation.Sector(0).Count, _expSharpeRatio[0], totalRevenues, _totalPotensialSales);
                    _fileMacro.Flush();

                    for (int i = 0; i < _settings.NumberOfSectors; i++)
                    {
                        _fileSectors.WriteLineTab(_scenario_id, Environment.MachineName, _runName, _time.Now, i,
                        _marketPrice[i], _marketWage[i], _marketPriceTotal, _marketWageTotal, _employment[i], _production[i],
                        _sales[i], _expSharpeRatio[i], _simulation.Sector(i).Count);
                    }                    
                    _fileSectors.Flush();

                    _nFirmCloseNatural = 0;
                    _nFirmCloseTooBig = 0;
                    _nFirmCloseNegativeProfit = 0;
                    _nFirmCloseZeroEmployment = 0;
                    _nFirmNew = 0;

                    Console.WriteLine("{0:#.##}\t{1}\t{2}\t{3:#.###}\t{4:#.###}\t{5:#.####}\t{6:#.####}\t{7:#.#}\t{8:#.#}", 1.0 * _settings.StartYear + 1.0 * _time.Now / _settings.PeriodsPerYear,
                        n_firms, _simulation.Households.Count, _marketWageTotal, _marketPriceTotal, consLoss, _avrProductivity, _totalSales/1000, totalConsumption/1000);  //h_rejectionRate
                    break;
                    #endregion

                case Event.System.Stop:
                    #region Event.System.Stop
                    CloseFiles();
                    if(!_settings.SaveScenario)
                    {
                        Console.WriteLine("\nRunning R-scripts:");

                        Console.WriteLine("-- macro.R..");
                        RunRScript("..\\..\\..\\R\\macro.R");

                        Console.WriteLine("-- macro_q.R..");
                        RunRScript("..\\..\\..\\R\\macro_q.R");

                        Console.WriteLine("-- firm_reports.R..");
                        RunRScript("..\\..\\..\\R\\firm_reports.R");
                    }
                    break;
                    #endregion

                default:
                    base.EventProc(idEvent);
                    break;
            }
        }
        #endregion
            
        #region Communicate
        public void Communicate(EStatistics comID, object o)
        {
            Firm f = null;
            Household h = null;
            switch (comID)
            {
                case EStatistics.FirmCloseNatural:
                    _nFirmCloseNatural++;
                    f = (Firm)o;
                    _firmInfo.Add(new FirmInfo(f));
                    return;

                case EStatistics.FirmCloseTooBig:
                    _nFirmCloseTooBig++;
                    f = (Firm)o;
                    _firmInfo.Add(new FirmInfo(f));
                    return;

                case EStatistics.FirmCloseNegativeProfit:
                    _nFirmCloseNegativeProfit++;
                    f = (Firm)o;
                    _firmInfo.Add(new FirmInfo(f));
                    return;

                case EStatistics.FirmCloseZeroEmployment:
                    _nFirmCloseZeroEmployment++;
                    f = (Firm)o;
                    _firmInfo.Add(new FirmInfo(f));
                    return;

                case EStatistics.Death:
                    //_totalProfitFromDefaults += (double)o;
                    return;

                case EStatistics.Profit:
                    f = (Firm)o;
                    _firmInfo.Add(new FirmInfo(f));
                    return;

                case EStatistics.FirmNew:
                    _nFirmNew += (double)o;
                    return;

                case EStatistics.CouldNotFindSupplier:
                    _n_couldNotFindSupplier ++;
                    return;

                case EStatistics.ChangeShopInSearchForShop:
                    _nChangeShopInSearchForShop++;
                    return;

                case EStatistics.ChangeShopInBuyFromShopNull:
                    _nChangeShopInBuyFromShopNull++;
                    return;
                    
                case EStatistics.ChangeShopInBuyFromShopLookingForGoods:
                    _nChangeShopInBuyFromShopLookingForGoods++;
                    return;

                case EStatistics.CouldNotFindFirmWithGoods:
                    _nCouldNotFindFirmWithGoods++;
                    return;

                case EStatistics.BuyFromShop:
                    _nBuyFromShop++;
                    return;

                case EStatistics.SuccesfullTrade:
                    _nSuccesfullTrade++;
                    return;

                case EStatistics.ZeroBudget:
                    _nZeroBudget++;
                    return;

                case EStatistics.SuccesfullTradeNonZero:
                    _nSuccesfullTradeNonZero++;
                    return;

                default:
                    return;
            }
        }
        #endregion

        #region Internal methods
        #region Write()
        void Write()
        {
            _fileDBStatistics.WriteLineTab(_expSharpeRatioTotal, _macroProductivity, _marketPrice, _marketWage);

        }
        #endregion

        #region RunRScript()
        void RunRScript(string fileName)
        {

            Process r = new();

            r.StartInfo.FileName = _settings.RExe;
            r.StartInfo.Arguments = "CMD BATCH " + fileName;
            r.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            r.Start();
            r.WaitForExit();

            //            Thread.Sleep(100);

        }
        #endregion

        #region OpenFiles()
        void OpenFiles()
        {
            if (!_settings.SaveScenario)
            {
                string path = _settings.ROutputDir + "\\data_year.txt";
                if (File.Exists(path)) File.Delete(path);
                using (StreamWriter sw = File.CreateText(path))
                    sw.WriteLine("Year\tPeriod\tn_Households\tavr_productivity\tnUnemployed\tnOptimalEmplotment\tP_star\tnEmployment\tnVacancies\tWage\tPrice\t" +
                        "Sales\tProfitPerHousehold\tnFirms\tProfitPerFirm\tMeanAge\tMeanValue\tnFirmCloseNatural\tnFirmCloseNegativeProfit\tnFirmCloseTooBig\t" +
                        "nFirmNew\tDiscountedProfits\tExpDiscountedProfits\tSharpeRatio\tExpSharpRatio\tLaborSupply\tYearConsumption\tYearEmployment\t" +
                        "PotensialSales\tProduction\tConsumption\tConsumptionValue\tConsumptionBudget\tnChangeShopInSearchForShop\t" +
                        "nChangeShopInBuyFromShopNull\tnChangeShopInBuyFromShopLookingForGoods\tnCouldNotFindFirmWithGoods\tnBuyFromShop\tnSuccesfullTrade\t" +
                        "nZeroBudget\tnSuccesfullTradeNonZero");

                path = _settings.ROutputDir + "\\sector_year.txt";
                if (File.Exists(path)) File.Delete(path);
                using (StreamWriter sw = File.CreateText(path))
                    sw.WriteLine("Time\tSector\tPrice\tWage\tEmployment\tProduction\tSales\tnFirm\texpSharpeRatio\texpSharpeRatioTotal"); 

                path = _settings.ROutputDir + "\\file_reports.txt"; // Ret til firms !!!!!!!!!!!!!!!!!!!!!!!!!!!!
                if (File.Exists(path)) File.Delete(path);
                _fileFirmReport = File.CreateText(path);
                _fileFirmReport.WriteLine("Time\tID\tProductivity\tEmployment\tProduction\tSales\tVacancies\tExpectedPrice\tExpectedWage\tPrice\tWage\tApplications" +
                    "\tQuitters\tProfit\tValue\tPotensialSales\tOptimalEmployment\tOptimalProduction\tExpectedSales\texpApplications\texpQuitters\texpAvrProd\tMarketPrice" +
                    "\tMarketWage\tExpectedPotentialSales\tExpectedEmployment\tEmploymentMarkup");

                

                path = _settings.ROutputDir + "\\household_reports.txt";
                if (File.Exists(path)) File.Delete(path);
                _fileHouseholdReport = File.CreateText(path);
                _fileHouseholdReport.WriteLine("Time\tID\tProductivity\tAge\tConsumption\tValConsumption\tIncome\tWealth\tWage\tP_macro");

                path = _settings.ROutputDir + "\\output.txt";
                if (!File.Exists(path))
                    using (StreamWriter sw = File.CreateText(path))
                        sw.WriteLine("n_firms\tPrice\tWage\tDiscountedProfits");

            }

            string macroPath = _settings.ROutputDir + "\\macro.txt";
            string sectorsPath = _settings.ROutputDir + "\\sectors.txt";
            string settingsPath = _settings.ROutputDir + "\\settings.json";

            #region Scenario-stuff
            if (_settings.SaveScenario)
            {
                if(_settings.NewScenarioDirs)
                {
                    Directory.CreateDirectory(_settings.ROutputDir + "\\Scenarios");
                    Directory.CreateDirectory(_settings.ROutputDir + "\\Scenarios\\Macro");
                    Directory.CreateDirectory(_settings.ROutputDir + "\\Scenarios\\Sectors");
                    Directory.CreateDirectory(_settings.ROutputDir + "\\Scenarios\\Settings");
                }

                string scnPath = _settings.ROutputDir + "\\scenario_info.txt";
                if (_settings.Shock == EShock.Nothing) // Base run
                {
                    if (!File.Exists(scnPath))
                        _scenario_id = 1;
                    else
                    {
                        using (StreamReader sr = File.OpenText(scnPath))
                            _scenario_id = Int32.Parse(sr.ReadLine());
                        _scenario_id++;
                        Console.WriteLine("Base: {0}, {1}", _scenario_id, _simulation.Seed); // Save seed so it can be used in shocks

                    }

                    if (File.Exists(scnPath)) File.Delete(scnPath);
                    using (StreamWriter sw = File.CreateText(scnPath))
                    {
                        sw.WriteLine("{0}", _scenario_id);
                        sw.WriteLine("{0}", _simulation.Seed);
                    }

                    macroPath = _settings.ROutputDir + "\\Scenarios\\Macro\\base_" + _scenario_id.ToString() + "_" + Environment.MachineName + ".txt";
                    sectorsPath = _settings.ROutputDir + "\\Scenarios\\Sectors\\base_" + _scenario_id.ToString() + "_" + Environment.MachineName + ".txt";
                    settingsPath = _settings.ROutputDir + "\\Scenarios\\Settings\\base_" + _scenario_id.ToString() + "_" + Environment.MachineName + ".json";

                }
                else //Counterfactual
                {
                    
                    using (StreamReader sr = File.OpenText(scnPath))
                        _scenario_id = Int32.Parse(sr.ReadLine());

                    _runName = _settings.Shock.ToString();                                    
                    macroPath = _settings.ROutputDir + "\\Scenarios\\Macro\\count_"  + _runName + "_" + _scenario_id.ToString() + "_" + Environment.MachineName + ".txt";
                    sectorsPath = _settings.ROutputDir + "\\Scenarios\\Sectors\\count_" + _runName + "_" + _scenario_id.ToString() + "_" + Environment.MachineName + ".txt";
                    settingsPath = _settings.ROutputDir + "\\Scenarios\\Settings\\count_" + _runName + "_" + _scenario_id.ToString() + "_" + Environment.MachineName + ".json";
                    Console.WriteLine("{0}: {1}, {2}", _runName, _scenario_id, _simulation.Seed);

                }
            }
            #endregion

            if (File.Exists(macroPath)) File.Delete(macroPath);
            _fileMacro = File.CreateText(macroPath);
            _fileMacro.WriteLine("Scenario\tMachine\tRun\tTime\texpSharpeRatio\tmacroProductivity\tmarketPrice\t" +
                   "marketWage\tnFirms\tEmployment\tSales\tLaborSupply\tnLaborSupply\tnUnemployed\t" +
                   "Production\tnHouseholds\tnFirmNew\tnFirmClosed\tSigmaRisk\tSharpeRatio\tMeanAge\t" +
                   "Vacancies\tmarketPrice0\tmarketWage0\temployment0\tsales0\tnFirm0\texpShapeRatio0\ttotalRevenues\tPotensialSales");

            if (File.Exists(sectorsPath)) File.Delete(sectorsPath);
            _fileSectors = File.CreateText(sectorsPath);
            _fileSectors.WriteLine("Scenario\tMachine\tRun\tTime\tSector\tPrice\tWage\tPriceTotal\tWageTotal\tEmployment\tProduction\tSales\tExpShapeRatio\tnFirm");

            if (File.Exists(settingsPath)) File.Delete(settingsPath);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string sJson = JsonSerializer.Serialize<Settings>(_settings, options);
            File.WriteAllText(settingsPath, sJson);
            File.WriteAllText(_settings.ROutputDir + "\\last_json.json", sJson);

        }
        #endregion

        #region CloseFiles()
        void CloseFiles()
        {
            if (!_settings.SaveScenario)
            {
                _fileFirmReport.Close();
                _fileHouseholdReport.Close();
                _fileMacro.Close();
                _fileSectors.Close();
            }
        }
        #endregion
        #endregion

        #region Public proporties
        public double[] PublicMarketWage
        {
            get { return _marketWage; }
        }
        public double PublicMarketWageTotal
        {
            get { return _marketWageTotal; }
        }
        public double PublicMarketPriceTotal
        {
            get { return _marketPriceTotal; }
        }

        public double[] PublicMarketPrice
        {
            get { return _marketPrice; }
        }

        public double PublicProductivity
        {
            get { return _macroProductivity; }
        }
        public double[] PublicSectorProductivity
        {
            get { return _sectorProductivity; }
        }
        public double PublicProfitPerHousehold
        {
            get { return _profitPerHousehold; }
        }
        public double PublicMeanValue
        {
            get { return _meanValue; }
        }
        public double PublicAverageProductivity
        {
            get { return _avrProductivity; }
        }

        public double PublicExpectedProfitPerFirm
        {
            get { return _expProfit; }
        }
        public double PublicDiscountedProfits
        {
            get { return _discountedProfits; }
        }
        public double PublicExpectedDiscountedProfits
        {
            get { return _expDiscountedProfits; }
        }
        public double PublicExpectedSharpRatioTotal
        {
            get { return _expSharpeRatioTotal; }
        }
        public double[] PublicExpectedSharpRatio
        {
            get { return _expSharpeRatio; }
        }
        public double PublicSharpRatioTotal
        {
            get { return _sharpeRatioTotal; }
        }
        public double PublicInterestRate
        {
            get { return _interestRate; }
        }

        public StreamWriter StreamWriterFirmReport
        {
            get { return _fileFirmReport; }
        }
        public StreamWriter StreamWriterHouseholdReport
        {
            get { return _fileHouseholdReport; }
        }
        public StreamWriter StreamWriterDBHouseholds
        {
            get { return _fileDBHouseholds; }
        }
        public StreamWriter StreamWriterDBFirms
        {
            get { return _fileDBFirms; }
        }

        #endregion

    }
}



