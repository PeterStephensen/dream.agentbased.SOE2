using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dream.AgentClass;
using Dream.IO;

namespace Dream.Models.SOE_Basic
{
    public class Firm : Agent
    {

        #region Private fields
        Simulation _simulation;
        Settings _settings;
        Time _time;
        Random _random;
        Statistics _statistics;
        
        double _phi0, _phi; // Productivity
        double _l_primo;   // Primo employment
        double _l_optimal;   // Optimal employment
        double _v_primo;   // Primo vacancies
        double _vacancies = 0;
        double _y_primo;   // Primo production
        double _y_optimal;   // Optimal productions
        double _profit_optimal;   // Optimal productions
        double _s_primo;   // Primo sales
        double _sales;   // Actual sales
        double _potentialSales;   // Actual sales
        double _profit, _p, _w; // Profits, price, wage 
        double _w_full;
        double _value = 0;
        List<Household> _employed;
        int _jobApplications = 0;
        int _jobQuitters = 0;
        double _expPrice = 1.0;
        double _expWage = 1.0;
        double _expWage_lag = 1.0;
        double _expApplications = 0;
        double _expQuitters = 0;
        double _expAvrProd = 0;
        //double _expProfit = 0;
        double _expSales = 0, _expPotentialSales = 0; 
        int _age = 0;
        bool _report = false;
        double _year = 0;
        int _sector=0;
        //double _l_wage;
        //double _b_wage=0;
        //double _l_price;
        //double _b_price=0;
        int _ok, _no;
        object _returnObject;
        bool _open = false;
        double _wageSavedDeath = 0; 
        #endregion

        #region Constructors
        public Firm(TabFileReader file) : this(0)
        {
            _age = file.GetInt32("Age");
            _phi0 = file.GetDouble("phi0");
            _expWage = file.GetDouble("expWage");
            _expPrice = file.GetDouble("expPrice");
            _w = file.GetDouble("w");
            _p = file.GetDouble("p");
            _sales = file.GetDouble("Sales");
            _expQuitters = file.GetDouble("expQuitters");
            _expApplications = file.GetDouble("expApplications");
            _expSales = file.GetDouble("expSales");
            _profit = file.GetDouble("Profit");

            _report = true;
            //_startFromDatabase = true;  
        }


        public Firm(List<Household> employed, int sector) : this(sector)
        {
            _employed = employed;

        }
        public Firm(int sector)
        {
            _simulation = Simulation.Instance;
            _settings = _simulation.Settings;
            _statistics = _simulation.Statistics;
            _time = _simulation.Time;
            _random = _simulation.Random;
            
            _employed = new List<Household>();
            _sector = sector;

            //_phi0 = _random.NextPareto(_settings.FirmParetoMinPhi, _settings.FirmPareto_k);
            double g_m = Math.Pow(1 + _settings.FirmProductivityGrowth, 1.0 / _settings.PeriodsPerYear) - 1.0;
            _phi0 = _random.NextPareto(_settings.FirmParetoMinPhi, _settings.FirmPareto_k) * Math.Pow(1 + g_m,_time.Now); //!!!!!!!!!!!!!!!!
            _phi = _phi0;
            
            if (_random.NextEvent(_settings.StatisticsFirmReportSampleSize))
                _report = true;

            _expWage = _statistics.PublicMarketWage[_sector];
            _expWage_lag = _statistics.PublicMarketWage[_sector];
            _expPrice = _statistics.PublicMarketPrice[_sector];
            _expAvrProd = 1.0;

            _w = _statistics.PublicMarketWage[_sector];
            _p = _statistics.PublicMarketPrice[_sector];
            //_l_price = Math.Log(_p);
            //_l_wage = Math.Log(_w);

        }
        #endregion

        #region EventProc
        public override void EventProc(int idEvent)
        {
            switch (idEvent)
            {
                case Event.System.Start:
                    #region Event.System.Start
                    // If initial firm
                    _phi0 = _random.NextPareto(_settings.FirmParetoMinPhiInitial, _settings.FirmPareto_k);
                    _phi = _phi0;
                    break;
                    #endregion

                case Event.System.PeriodStart:
                    #region Event.System.PeriodStart
                    _year = 1.0 * _settings.StartYear + 1.0 * _time.Now / _settings.PeriodsPerYear;
                    ReportToStatistics();

                    _phi = _phi0 * _statistics.PublicProductivity * _statistics.PublicSectorProductivity[_sector];

                    _l_primo = CalcEmployment(); // Primo employment
                    _s_primo = _sales;

                    Expectations();
                    Produce();
                    Management();
                    Marketing();
                    HumanResource();

                    // Initialize
                    _jobApplications = 0;
                    _jobQuitters = 0;
                    _sales = 0;
                    _potentialSales = 0;
                    _wageSavedDeath = 0;
                    _no = 0;
                    _ok = 0;
                    _w_full = _w;

                    // Shock: Tsunami shock
                    if (_time.Now == _settings.ShockPeriod)
                        if(_settings.Shock==EShock.Tsunami)
                            if (_random.NextEvent(0.1))
                                CloseFirm(EStatistics.FirmCloseNatural);

                    if (_age == _settings.FirmStartupPeriod)
                        _open = true;    

                    break;
                    #endregion

                case Event.Economics.Update:
                    #region Event.Economics.Update
                    // Default
                    if (_time.Now > _settings.FirmDefaultStart)  // 12*5
                        if (_time.Now > _settings.BurnInPeriod1)  //2030
                            if (_age > _settings.FirmStartupPeriod)  //6
                            {
                                if (_profit_optimal <= 0 | _employed.Count == 0)
                                    if (_random.NextEvent(_settings.FirmDefaultProbabilityNegativeProfit)) //0.5
                                    {
                                        CloseFirm(EStatistics.FirmCloseZeroEmployment);
                                        break;
                                    }
                            }

                    if (_random.NextEvent(_settings.FirmDefaultProbability))
                    {
                        CloseFirm(EStatistics.FirmCloseNatural);
                        break;
                    }

                    //Firings
                    if (_time.Now > _settings.FirmFiringsStart)
                    {
                        double l = CalcEmployment();
                        // Last in - First out
                        while (_employed.Count>0 && _l_optimal < l - _employed.Last().Productivity) // Kan det betale sig at fyre sidst ansatte medarbejder?
                        {
                            Household h = _employed.Last();
                            h.Communicate(ECommunicate.YouAreFired, this);
                            l -= h.Productivity;
                            _employed.Remove(h);
                        }
                    }
                    break;
                    #endregion

                case Event.System.PeriodEnd:
                    #region Event.System.PeriodEnd
                    if (_time.Now == _settings.StatisticsWritePeriode)
                        Write();
                    
                    _profit = _p * _sales - _w_full * _l_primo + _wageSavedDeath;                        
                    
                    _statistics.Communicate(EStatistics.Profit, this);

                    if (_time.Now > 4)
                        _value = (1 + _statistics.PublicInterestRate) * _value + _profit;

                    _age++;
                    break;
                    #endregion

                case Event.System.Stop:
                    break;

                default:
                    base.EventProc(idEvent);
                    break;
            }
        }
        #endregion

        #region Communicate
        public ECommunicate Communicate(ECommunicate comID, object o)
        {
            _returnObject = null;
            Household h = null;
            switch (comID)
            {
                case ECommunicate.JobApplication:
                    _jobApplications++;
                    if (_vacancies>0)
                    {
                        h = (Household)o;
                        _employed.Add(h);
                        _vacancies -= h.Productivity;
                        _vacancies = _vacancies < 0 ? 0 : _vacancies;
                        return ECommunicate.Yes;
                    }
                    return ECommunicate.No;

                case ECommunicate.IQuit:
                    _jobQuitters++;
                    _employed.Remove((Household)o);
                    return ECommunicate.Ok;

                case ECommunicate.Death:
                    _jobQuitters++;
                    h = (Household)o;
                    _wageSavedDeath += _w * h.Productivity;
                    _employed.Remove(h);
                    return ECommunicate.Ok;

                case ECommunicate.CanIBuy:
                    double x = (double)o;
                    if (x < 0)
                        throw new Exception("Can only buy positive number.");
                    
                    _w_full = _w;  // Remove this!!!
                    _potentialSales += x;
                    if (_sales + x <= _y_primo)
                    {
                        _sales += x;
                        _ok++;
                        return ECommunicate.Yes;
                    }
                    else
                    {
                        _returnObject = _y_primo - _sales;
                        _sales = _y_primo;
                        _no++;
                        return ECommunicate.No;
                    }

                case ECommunicate.Initialize:
                    _employed.Add((Household)o);
                    return ECommunicate.Ok;               
                
                default:
                    return ECommunicate.Ok;
            }
        }
        #endregion

        #region Write()
        void Write()
        {

            if (!_settings.SaveScenario)
                _statistics.StreamWriterDBFirms.WriteLineTab(ID, _age, _phi0, _expPrice, _expWage, _expQuitters, 
                _expApplications, _expPotentialSales, _expSales, _w, _p, _sales, _profit);


        }
        #endregion

        #region Internal methods
        #region Expectations()
        void Expectations()
        {

            _expWage_lag = _expWage;

            _expPrice = _settings.FirmExpectationSmooth * _expPrice + (1 - _settings.FirmExpectationSmooth) * _statistics.PublicMarketPrice[_sector];
            _expWage = _settings.FirmExpectationSmooth * _expWage + (1 - _settings.FirmExpectationSmooth) * _statistics.PublicMarketWageTotal;
            _expQuitters = _settings.FirmExpectationSmooth * _expQuitters + (1 - _settings.FirmExpectationSmooth) * _jobQuitters;
            _expApplications = _settings.FirmExpectationSmooth * _expApplications + (1 - _settings.FirmExpectationSmooth) * _jobApplications;
            //_expApplications = 0.99 * _expApplications + (1 - 0.99) * _jobApplications;
            _expPotentialSales = _settings.FirmExpectationSmooth * _expPotentialSales + (1 - _settings.FirmExpectationSmooth) * _potentialSales;
            _expSales = _settings.FirmExpectationSmooth * _expSales + (1 - _settings.FirmExpectationSmooth) * _sales;

        }
        #endregion

        #region Production()
        /// <summary>
        /// Actual production in this period
        /// </summary>
        void Produce()
        {
            double z = Math.Pow(_l_primo, _settings.FirmAlpha) - _settings.FirmFi;
            _y_primo = z > 0 ? _phi * z : 0;

            if (_age < _settings.FirmStartupPeriod)
                _y_primo = 0;
        }
        #endregion

        #region Management()
        /// <summary>
        /// Planning: Choosing optimal employment end production
        /// </summary>
        void Management()
        {
            double alpha = _settings.FirmAlpha;
            double fi = _settings.FirmFi;
            double gamma_y = _settings.FirmGamma_y;

            if(_age < _settings.FirmStartupPeriod)
            {
                _l_optimal = _settings.FirmStartupEmployment;
                _y_optimal = 0;
                _profit_optimal = 0;
                return;
            }

            _l_optimal = Math.Pow(alpha * _phi * gamma_y * _expPrice / _expWage, 1 / (1 - alpha)); // Optimal employment

            if (_l_optimal > _settings.FirmMaxEmployment)
            {
                foreach (Household h in _employed)
                    h.Communicate(ECommunicate.YouAreFired, this);

                _statistics.Communicate(EStatistics.FirmCloseTooBig, this);
                RemoveThisAgent();
                return;
            }

            double z = Math.Pow(_l_optimal, alpha);
            if (z > fi)
            {
                _y_optimal = _phi * (z - fi);
                _profit_optimal = _expPrice * _y_optimal * gamma_y - _expWage * _l_optimal;
            }
            else
            {
                _l_optimal = 0;
                _y_optimal = 0;
                _profit_optimal = 0;
            }

        }
        #endregion

        #region Marketing()
        /// <summary>
        /// Selling the good: Setting price
        /// </summary>


        void Marketing()
        {

            bool inZone = Math.Abs((_expSales - _y_optimal) / _y_optimal) < _settings.FirmComfortZoneSales;
            double probRecalculate = inZone ? _settings.FirmProbabilityRecalculatePriceInZone : _settings.FirmProbabilityRecalculatePrice;
            double gamma_y = _settings.FirmGamma_y;
            bool advertise = false;

            if (_random.NextEvent(probRecalculate))
            {

                double p_target = _expPrice;

                if (_time.Now > _settings.FirmPriceMechanismStart & _age >= _settings.FirmStartupPeriod)  
                {

                    if (_y_primo > 0)
                    {
                        double markup = _settings.FirmPriceMarkup;
                        double markdown = _settings.FirmPriceMarkdown;
                        double markupSensitivity = _settings.FirmPriceMarkupSensitivity;
                        double markdownSensitivity = _settings.FirmPriceMarkdownSensitivity;

                        if(inZone)
                        {
                            markup = _settings.FirmPriceMarkupInZone;
                            markdown = _settings.FirmPriceMarkdownInZone;
                            markupSensitivity = _settings.FirmPriceMarkupSensitivityInZone;
                            markdownSensitivity = _settings.FirmPriceMarkdownSensitivityInZone;
                        }
                       
                        //if (_expSales < _y_optimal)
                        if (_expSales < _y_primo * gamma_y)
                        {
                            //double g = markdown * PriceFunc(markdownSensitivity * (_y_optimal - _expSales) / _y_optimal);
                            double g = markdown * PriceFunc(markdownSensitivity * (_y_primo * gamma_y - _expSales) / (_y_primo * gamma_y));
                            p_target = (1 - g) * _expPrice;
                            
                            if(_age<12*5)
                                advertise = true;
                        }
                        else if (_expPotentialSales / _settings.FirmExpectedExcessPotentialSales > _y_primo )
                        {
                            double g = markup * PriceFunc(markupSensitivity * (_expPotentialSales / _settings.FirmExpectedExcessPotentialSales - _y_primo) / _y_primo);
                            p_target = (1 + g) * _expPrice;
                        }
                        //else if (_expPotentialSales > _y_optimal)
                        //{
                        //    double g = markup * PriceFunc(markupSensitivity * (_expPotentialSales - _y_optimal) / _y_optimal);
                        //    p_target = (1 + g) * _expPrice;
                        //}

                    }
                }

                double a = 0.8;
                _p = a * _p + (1 - a) * p_target;
                if (advertise)
                    AdvertiseGood();

            }
            else 
            {
                double a = 0.8;
                //_p = a * _p + (1 - a) * _expPrice; Very slow!
                //_p = _expPrice;
            }
        }
        
        double PriceFunc2(double x)
        {
            return Math.Atan(0.5 * Math.PI * x) / (0.5 * Math.PI);
        }
        double PriceFunc(double x)
        {            
            return x < 1 ? x : 1;
        }
        #endregion

        #region HumanResource()
        /// <summary>
        /// Setting wage and vacancies
        /// </summary>
        void HumanResource()
        {
            if (_age < _settings.FirmStartupPeriod)
            {
                //HumanResourceStartUp();
                //return;
            }

            bool advertise = false;
            double l = CalcEmployment();
            double avr_prod = 1.0;
            if(_employed.Count>0) avr_prod = l / _employed.Count;
            //_expAvrProd = _settings.FirmExpectationSmooth * _expAvrProd + (1 - _settings.FirmExpectationSmooth) * avr_prod;
            //_expAvrProd = avr_prod;
            //_expAvrProd = 1.05;
            _expAvrProd = _statistics.PublicAverageProductivity;

            _vacancies = 0;
            double l_target = _l_optimal;
            //if(_time.Now>12*50)
            //    l_target = 1.1 * _l_optimal;

            
            if (l_target > l)
            {
                double gamma = _age >= _settings.FirmStartupPeriod ? _settings.FirmVacanciesShare : 1.0; 
                
                if(l_target - l <= _settings.FirmMinRemainingVacancies)
                    _vacancies = l_target - l;
                else
                    _vacancies = gamma * (l_target - l);
            }

            _v_primo = _vacancies; // Primo vacancies

            bool inZone = Math.Abs((l - l_target) / l_target) < _settings.FirmComfortZoneEmployment;
            double probRecalculate = inZone ? _settings.FirmProbabilityRecalculateWageInZone : _settings.FirmProbabilityRecalculateWage;
            
            if (_random.NextEvent(probRecalculate))
            {

                double markup = _settings.FirmWageMarkup;
                double markdown = _settings.FirmWageMarkdown;
                double markupSensitivity = _settings.FirmWageMarkupSensitivity;
                double markdownSensitivity = _settings.FirmWageMarkdownSensitivity;

                if (inZone)
                {
                    markup = _settings.FirmWageMarkupInZone;
                    markdown = _settings.FirmWageMarkdownInZone;
                    markupSensitivity = _settings.FirmWageMarkupSensitivityInZone;
                    markdownSensitivity = _settings.FirmWageMarkdownSensitivityInZone;
                }

                // Wage formation
                double w_target = _expWage;
                //_w = _expWage;

                //if (_expApplications * _expAvrProd < _vacancies + _expQuitters * _expAvrProd)
                //{
                //    double g = markup * PriceFunc(markupSensitivity * (_vacancies + _expQuitters * _expAvrProd - _expApplications * _expAvrProd) / _l_optimal);// _employed.Count);
                //    w_target = (1 + g) * _expWage;
                //}
                //else
                //{
                //    double g = markdown * PriceFunc(markdownSensitivity * (_expApplications * _expAvrProd - _vacancies - _expQuitters * _expAvrProd) / _l_optimal);// _employed.Count);
                //    w_target = (1 + g) * _expWage;
                //}


                if (_vacancies > 0)
                {
                    if (_expApplications * _expAvrProd < _vacancies + _expQuitters * _expAvrProd)
                    {
                        double g = markup * PriceFunc(markupSensitivity * (_vacancies + _expQuitters * _expAvrProd - _expApplications * _expAvrProd) / _l_optimal);// _employed.Count);
                        //double g = markupSensitivity * (_vacancies + _expQuitters * _expAvrProd - _expApplications * _expAvrProd) / _l_optimal;
                        w_target = (1 + g) * _expWage;
                        if (_age < 12 * 5)
                            advertise = true;
                    }
                }
                else
                {
                    if (_expApplications > _expQuitters)
                    {
                        double g = markdown * PriceFunc(markdownSensitivity * (_expApplications * _expAvrProd - _expQuitters * _expAvrProd) / _l_optimal);// _employed.Count);
                        //double g = markdownSensitivity * (_expApplications * _expAvrProd - _expQuitters * _expAvrProd) / _l_optimal;
                        w_target = (1 - g) * _expWage;
                    }
                }

                double a = 0.8;
                _w = a * _w + (1 - a) * w_target;
                if (advertise)
                    AdvertiseJob();
            }
            else
            {
                double a = 0.8;
                //_w = a * _w + (1 - a) * _expWage; Very slow!
                //_w = _expWage;
            }
        }

        //void HumanResourceStartUp()
        //{
        //    double l = CalcEmployment();

        //    _w = _expWage;

        //    if (l<_settings.FirmStartupEmployment)   // Forced employment!
        //    {
        //        while(l < _settings.FirmStartupEmployment)
        //        {
        //            Household h = _simulation.GetRandomHousehold();
        //            if(h.Age<_settings.HouseholdPensionAge)
        //            {
        //                h.Communicate(ECommunicate.YouAreHiredInStartup, this);
        //                l += h.Productivity;
        //                _employed.Add(h);
        //            }
        //        }                
        //    }
        //}



        #endregion

        #region Advertising
        void AdvertiseGood()
        {
            foreach (var h in _simulation.GetRandomHouseholds(_settings.FirmNumberOfGoodAdvertisements))
                h.Communicate(ECommunicate.AvertiseGood, this);

        }

        void AdvertiseJob()
        {

            foreach (var h in _simulation.GetRandomHouseholds(_settings.FirmNumberOfJobAdvertisements))
                h.Communicate(ECommunicate.AvertiseJob, this);

        }
        #endregion

        #region ReportToStatistics()
        void ReportToStatistics()
        {
            if (_report & !_settings.SaveScenario)
            {
                //_statistics.StreamWriterFirmReport.WriteLineTab(_year, ID, _phi, _l_primo, _y_primo, _s_primo, 
                //    _v_primo, _expPrice, _expWage, _p, _w, _jobApplications, _jobQuitters, _profit, _value, _potentialSales, _l_optimal, _y_optimal, _expSales);

                _statistics.StreamWriterFirmReport.WriteLineTab(_year, ID, _phi, _l_primo, _y_primo, _s_primo,
                      _v_primo, _expPrice, _expWage, _p, _w, _jobApplications, _jobQuitters, _profit, _value, _potentialSales, _l_optimal, _y_optimal, _expSales,
                      _expApplications, _expQuitters, _expAvrProd);


                _statistics.StreamWriterFirmReport.Flush();

            }


        }
        #endregion

        #region CloseFirm()
        void CloseFirm(EStatistics s)
        {
            _profit = _p * _sales - _w * _l_primo + _wageSavedDeath;

            // Fire eveybody
            foreach (Household h in _employed)
                h.Communicate(ECommunicate.YouAreFired, this);

            _statistics.Communicate(s, this);
            RemoveThisAgent();

        }
        #endregion

        #region CalcEmployment()
        /// <summary>
        /// Calculate employment as sum over productivities. Do not use too often!!!
        /// </summary>
        /// <returns>Employment measured in productivity units</returns>
        double CalcEmployment()
        {
            return _employed.Sum(e => e.Productivity);
        }
        #endregion
        #endregion
        
        #region Public proporties
        public bool Open
        {
            get { return _open; }
        }
        public object ReturnObject
        {
            get { return _returnObject; }
        }
        public double Productivity
        {
            get { return _phi; }
        }
        public double OptimalEmployment
        {
            get { return _l_optimal; }
        }
        public double OptimalProduction
        {
            get { return _y_optimal; }
        }
        public double OptimalProfit
        {
            get { return _profit_optimal; }
        }
        public double Production
        {
            get { return _y_primo; }
        }
        public double Profit
        {
            get { return _profit; }
        }
        public double Wage
        {
            get { return _w; }
        }
        public double FullWage
        {
            get { return _w_full; }
        }
        public int Sector
        {
            get { return _sector; }
        }
        public double Price
        {
            get { return _p; }
        }

        /// <summary>
        /// Primo employment
        /// </summary>
        public double Employment
        {
            get { return _l_primo; }
        }
        /// <summary>
        /// Primo vacancies
        /// </summary>
        public double Vacancies
        {
            get { return _v_primo; }
        }
        public int JobApplications
        {
            get { return _jobApplications; }
        }
        public int JobQuitters
        {
            get { return _jobQuitters; }
        }
        public double Sales
        {
            //get { return _s_primo; }
            get {return _sales; }
        }
        public double PotentialSales
        {
            //get { return _s_primo; }
            get { return _potentialSales; }
        }
        public double Value
        {
            get { return _value; }
        }

        public int Age
        {
            get { return _age; }
        }
        public int NumberOfEmployed
        {
            get { return _employed.Count; }
        }
        public int NumberOfNo
        {
            get { return _no; }
        }
        public int NumberOfOK
        {
            get { return _ok; }
        }

        /// <summary>
        /// Used as placeholder in household calculations 
        /// </summary>
        public double Utility { get; set; } = 0;

        #endregion
    
    }
}

