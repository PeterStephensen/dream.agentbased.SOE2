using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dream.AgentClass;
using Dream.IO;  

namespace Dream.Models.SOE_Basic
{
    public class Household : Agent
    {

        #region Private fields
        Simulation _simulation;
        Settings _settings;
        Time _time;
        Random _random;
        Statistics _statistics;
        int _age;
        Firm _firmEmployment=null, _firmShop=null;
        //bool _unemp = false; // Primo: unemployed? 
        double _w = 0; //Wage
        int _unempDuration = 0;
        double _productivity = 0;
        bool _initialHousehold = false;
        double _yr_consumption = 0;
        int _yr_employment = 0;
        bool _startFromDatabase = false;
        bool _report = false;
        double _consumption = 0;
        double _income = 0;
        double _consumption_budget = 0;
        double _consumption_value = 0;
        double _wealth = 0;
        double _year;
        double[] _c = null;  // Consumption
        double[] _vc = null; // Value of consumption
        double[] _budget = null;
        Firm[] _firmShopArray = null;
        double[] _s_CES = null;
        double _P_CES = 0;
        bool _fired = false;
        int _no, _ok;
        //bool _dead = false;

        #endregion

        #region Constructors
        public Household()
        {

            _simulation = Simulation.Instance;
            _settings = _simulation.Settings;
            _time = _simulation.Time;
            _random = _simulation.Random;
            _statistics = _simulation.Statistics;
            
            _productivity = 1.0;
            _age = _settings.HouseholdStartAge;
            _c = new double[_settings.NumberOfSectors];
            _vc = new double[_settings.NumberOfSectors];
            _budget = new double[_settings.NumberOfSectors];
            _firmShopArray = new Firm[_settings.NumberOfSectors];
            _wealth = 0;

            _s_CES = new double[_settings.NumberOfSectors];
            for (int i = 0; i < _settings.NumberOfSectors; i++)   // Random share parameters in the CES-function
                _s_CES[i] = 1.0;  // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //_s_CES[i] = _random.NextDouble();

            if (_random.NextEvent(_settings.StatisticsHouseholdReportSampleSize))
                _report = true;


        }
        public Household(TabFileReader file) : this()
        {

            // WIP
            throw new NotImplementedException();
            
            _age = file.GetInt32("Age");
            _productivity = file.GetDouble("Productivity");

            int firmEmploymentID = file.GetInt32("FirmEmploymentID");
            int firmShopID = file.GetInt32("FirmShopID");
            
            if(firmEmploymentID != -1)
                _firmEmployment = _simulation.GetFirmFromID(firmEmploymentID);                        

            if(firmShopID != -1)            
                _firmShop = _simulation.GetFirmFromID(firmShopID);

            if(_firmEmployment != null)
                _firmEmployment.Communicate(ECommunicate.Initialize, this);

            _startFromDatabase = true;
            

        }
        #endregion

        #region EventProc
        public override void EventProc(int idEvent)
        {
            switch (idEvent)
            {

                case Event.System.Start:  // Initial households
                    if(_startFromDatabase)
                    {
                        _initialHousehold = false;
                    }
                    else
                    {
                        _age = _settings.HouseholdStartAge + (int)(_random.NextDouble() * (_settings.HouseholdPensionAge - _settings.HouseholdStartAge - 1));
                        _productivity = Math.Exp(_random.NextGaussian(_settings.HouseholdProductivityLogMeanInitial, _settings.HouseholdProductivityLogSigmaInitial));
                        _initialHousehold = true;
                    }
                    break;

                case Event.System.PeriodStart:
                    bool unemployed = _fired | _w == 0;  // Unemployed if just fired or if wage is zero
                    if (_fired) _fired = false; // Fired only 1 period

                    _year = _settings.StartYear + 1.0 * _time.Now / _settings.PeriodsPerYear;
                    ReportToStatistics();

                    _unempDuration = !unemployed ? 0 : _unempDuration+1;
                    if (_time.Now == 0) _w = _simulation.Statistics.PublicMarketWageTotal;

                    if(_time.Now % _settings.PeriodsPerYear==0)
                    {
                        _yr_consumption = 0;
                        _yr_employment = 0;
                    }

                    if (!unemployed) _yr_employment++;
                   
                    
                    if (unemployed) _w = 0;
                    _no = 0;
                    _ok = 0;
                    break;

                case Event.Economics.Update:
                    // Income calculadet here because PublicProfitPerHousehold is calculated during PeriodStart-event by the Statistics object
                    _income = _w * _productivity + _simulation.Statistics.PublicProfitPerHousehold;

                    #region Not used (Saving)
                    if (_age >= _settings.HouseholdPensionAge) // If pensioner
                    {
                        _consumption_budget = _settings.HouseholdDisSaveRatePensioner * _wealth;
                        _wealth = _wealth - _consumption_budget;
                    }
                    else if (_w > 0)                            // If employed
                    {
                        _consumption_budget = (1 - _settings.HouseholdSaveRate) * _income;
                        _wealth = _wealth + _settings.HouseholdSaveRate * _income;
                    }
                    else                                     // If unemployed 
                    {
                        _consumption_budget = _settings.HouseholdDisSaveRateUnemployed * _wealth;
                        _wealth = _wealth - _consumption_budget;
                    }
                    #endregion

                    // This overwrites code above *****************************************************
                    _consumption_budget = _income; // **************************************************


                    if (_age==_settings.HouseholdPensionAge)
                    {
                        if(_firmEmployment != null)
                        {
                            _firmEmployment.Communicate(ECommunicate.IQuit, this);
                            _firmEmployment = null;
                        }
                    }
                    else if(_age<_settings.HouseholdPensionAge)
                    {
                        if (_firmEmployment == null)  // If unemployed
                            SearchForJob();
                        else  // If employed
                        {
                            // If job is changed, it is from next period. 
                            if (_random.NextEvent(_settings.HouseholdProbabilitySearchForJob))
                                SearchForJob();

                            if (_random.NextEvent(_settings.HouseholdProbabilityQuitJob))
                            {
                                _firmEmployment.Communicate(ECommunicate.IQuit, this);
                                _firmEmployment = null;
                            }
                        }                    
                    }

                    for (int s = 0; s < _settings.NumberOfSectors; s++)
                        if (_random.NextEvent(_settings.HouseholdProbabilitySearchForShop))
                            SearchForShop(s);

                    BuyFromShops(); 
                    _yr_consumption += _consumption;

                    if (_random.NextEvent(ProbabilityDeath()))
                    {
                        if (_firmEmployment != null)
                        {
                            _firmEmployment.Communicate(ECommunicate.Death, this);
                            _statistics.Communicate(EStatistics.Death, _w * _productivity); // Wage earned this period
                        }

                        //Inheritance();
                        RemoveThisAgent();
                        return;
                    }
                    break;

                case Event.System.PeriodEnd:
                    if (_time.Now == _settings.StatisticsWritePeriode)
                        Write();


                    if (!_initialHousehold)
                        if (_age < _settings.HouseholdPensionAge)
                            _productivity *= Math.Exp(_random.NextGaussian(0, _settings.HouseholdProductivityErrorSigma));
                        else
                            _productivity = 0;

                    _w = _firmEmployment == null ? 0.0 : _firmEmployment.Wage;

                    _age++;
                    break;

                case Event.System.Stop:
                    break;

                default:
                    base.EventProc(idEvent);
                    break;
            }
        }
        #endregion

        #region Internal methods
        #region BuyFromShops
        void BuyFromShops()
        {
            // Calculate CES-priceindex
            _P_CES = 0;
            for (int s = 0; s < _settings.NumberOfSectors; s++)
            {
                if (_firmShopArray[s] == null)
                    _firmShopArray[s] = _simulation.GetRandomFirm(s);

                _P_CES += _s_CES[s] * Math.Pow(_firmShopArray[s].Price, 1 - _settings.HouseholdCES_Elasticity);
            }
            _P_CES = Math.Pow(_P_CES, 1 / (1 - _settings.HouseholdCES_Elasticity));

            // Calculate budget 
            for (int s = 0; s < _settings.NumberOfSectors; s++)
                _budget[s] = _s_CES[s] * Math.Pow(_firmShopArray[s].Price / _P_CES, 1 - _settings.HouseholdCES_Elasticity) * _consumption_budget;

            // Buy goods
            _consumption_value = 0;
            for (int s = 0; s < _settings.NumberOfSectors; s++)
            {
                int zz = 0;
                if (_time.Now > 12 * 30)
                    zz = 22;

                BuyFromShop(s);
                _consumption_value += _vc[s];
                
            }
        }

        #endregion

        #region BuyFromShop()

        void BuyFromShop(int sector)
        {
            if (_firmShopArray[sector] == null)
                _firmShopArray[sector] = _simulation.GetRandomFirm(sector); //SearchForShop ???????????????????????


           
            if (_budget[sector] < 0)
            {
                _c[sector] = 0;
                _vc[sector] = 0;
                return;
            }

            _ok++;
            if (_firmShopArray[sector].Communicate(ECommunicate.CanIBuy, _budget[sector] / _firmShopArray[sector].Price) == ECommunicate.Yes)
            {
                _c[sector] = _budget[sector] / _firmShopArray[sector].Price;
                _vc[sector] = _budget[sector];
                return;
            }
            else
            {
                double c = (double)_firmShopArray[sector].ReturnObject;
                _c[sector] = c;
                _vc[sector] = _firmShopArray[sector].Price * c;
                _budget[sector] -= _firmShopArray[sector].Price * c;

                Firm[] firms = _simulation.GetRandomFirms(_settings.HouseholdMaxNumberShops, sector);
                firms = firms.OrderBy(x => x.Price).ToArray<Firm>(); // Order by price. Lowest price first   // Speeder up!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                int pos = 0;
                foreach (Firm f in firms)
                {
                    if(f.Open)
                        if (f.Communicate(ECommunicate.CanIBuy, _budget[sector] / f.Price) == ECommunicate.Yes)
                        {
                            _firmShopArray[sector] = f;
                            _vc[sector] += _budget[sector];
                            _c[sector] += _budget[sector] / _firmShopArray[sector].Price;
                            return;
                        }
                        else
                        {
                            c = (double)_firmShopArray[sector].ReturnObject;
                            if (c > 0) pos++;
                            _vc[sector] += _firmShopArray[sector].Price * c;
                            _c[sector] += c;
                            _budget[sector] -= _firmShopArray[sector].Price * c;                            
                        }
                }
                int zz = 0;
                if (_time.Now > 12 * 30 & pos>0)
                    zz = 22;


                _no++;
                _ok--;
                //_vc[sector] = 0;
                //_c[sector] = 0;

                //_statistics.Communicate(EStatistics.Death, _budget[sector]); // Trick to get money back in circulation !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                _statistics.Communicate(EStatistics.CouldNotFindSupplier, this);
                return;

            }
        }

        void BuyFromShop_NEW(int sector)
        {
            if (_firmShopArray[sector] == null)
                _firmShopArray[sector] = _simulation.GetRandomFirm(sector); //SearchForShop ???????????????????????

            if (_budget[sector] < 0)
            {
                _vc[sector] = 0;
                _c[sector] = 0;
                return;
            }

            if (_firmShopArray[sector].Communicate(ECommunicate.CanIBuy, _budget[sector] / _firmShopArray[sector].Price) == ECommunicate.Yes)
            {
                _vc[sector] = _budget[sector];
                _c[sector] = _budget[sector] / _firmShopArray[sector].Price;
                _ok++;
                return;
            }
            else
            {
                _vc[sector] = _budget[sector];
                _c[sector] = _budget[sector] / _firmShopArray[sector].Price;
                _no++;


                List<Firm> firms = _simulation.GetRandomFirms(_settings.HouseholdMaxNumberShops, sector).ToList<Firm>();
                //Firm[] firms = _simulation.GetRandomFirms(_settings.HouseholdMaxNumberShops, sector);

                //firms = firms.OrderBy(x => x.Price).ToArray<Firm>(); // Order by price. Lowest price first   // Speeder up!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                for (int j = 0; j < 10; j++)
                {
                    Firm f = firms.MinBy(x => x.Price);
                    if (f.Communicate(ECommunicate.CanIBuy, _budget[sector] / f.Price) == ECommunicate.Yes)
                    {
                        _firmShopArray[sector] = f;
                        _vc[sector] = _budget[sector];
                        _c[sector] = _budget[sector] / _firmShopArray[sector].Price;
                        return;
                    }
                    firms.Remove(f);

                }

                _vc[sector] = 0;
                _c[sector] = 0;

                _statistics.Communicate(EStatistics.Death, _budget[sector]); // Trick to get money back in circulation !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                _statistics.Communicate(EStatistics.CouldNotFindSupplier, this);
                return;

            }
        }


        #endregion

        #region SearchForJob()
        void SearchForJob()
        {

            double wageNow = _firmEmployment != null ? _firmEmployment.Wage : 0.0;
            
            var firms = _simulation.GetRandomFirmsAllSectors(_settings.HouseholdNumberFirmsSearchJob);
            firms = firms.OrderByDescending(x => x.Wage).ToArray<Firm>(); // Order by wage. Highest wage first

            foreach (Firm f in firms)
            {
                if(f.Wage > wageNow)
                    if (f.Communicate(ECommunicate.JobApplication, this) == ECommunicate.Yes)
                    {
                        if (_firmEmployment != null)
                            _firmEmployment.Communicate(ECommunicate.IQuit, this);

                        _firmEmployment = f;
                        break;
                    }
            }
        }
        #endregion

        #region SearchForShop()
        void SearchForShop(int sector)
        {

            Firm[] firms = _simulation.GetRandomFirms(_settings.HouseholdNumberFirmsSearchShop, sector);
            firms = firms.OrderBy(x => x.Price).ToArray<Firm>(); // Order by price. Lowes price first

            if (_firmShopArray[sector] == null || firms.First().Price < _firmShopArray[sector].Price)
                _firmShopArray[sector] = firms.First();

        }

        #endregion

        #region Inheritance
        void Inheritance()
        {
            if (_wealth == 0)
                return;
            
            double inheritance = _wealth / _settings.NumberOfInheritors;
            int inh = 0;
            while(inh<_settings.NumberOfInheritors)
            {
                Household h = _simulation.GetRandomHousehold();
                if(h.Age<_settings.HouseholdPensionAge)
                {
                    h.Communicate(ECommunicate.Inheritance, inheritance);
                    inh++;
                }
            }
            _wealth = 0;

        }
        #endregion

        #region ProbabilityDeath()
        double ProbabilityDeath()
        {
            return Math.Pow(1 + Math.Exp(0.1 * _age / _settings.PeriodsPerYear - 10.0), 1.0/_settings.PeriodsPerYear) - 1;

        }
        #endregion

        #region ReportToStatistics()
        void ReportToStatistics()
        {
            if (_report & !_settings.SaveScenario)
            {               
                _statistics.StreamWriterHouseholdReport.WriteLineTab(_year, this.ID, _productivity, _age/ _settings.PeriodsPerYear, 
                    _consumption, _consumption_budget, _income, _wealth, _w, _statistics.PublicMarketPriceTotal);
                _statistics.StreamWriterHouseholdReport.Flush();
            }
        }
        #endregion

        #endregion

        #region Communicate
        public ECommunicate Communicate(ECommunicate comID, object o)
        {
            switch (comID)
            {
                case ECommunicate.YouAreFired:
                    _fired = true;
                    //_firmEmployment = null;
                    return ECommunicate.Ok;
               
                case ECommunicate.Initialize:
                    _firmEmployment = (Firm)o;
                    return ECommunicate.Ok;
                
                case ECommunicate.Inheritance:
                    _wealth += (double)o;
                    return ECommunicate.Ok;

                default:
                    return ECommunicate.Ok;
            }
        }
        #endregion

        #region Write()
        void Write()
        {

            int firmEmploymentID = _firmEmployment != null ? _firmEmployment.ID : -1;
            int firmShopID = _firmShop != null ? _firmShop.ID : -1;

            if (!_settings.SaveScenario)
                _statistics.StreamWriterDBHouseholds.WriteLineTab(ID, _age/ _settings.PeriodsPerYear, firmEmploymentID, firmShopID, _productivity);

        }
        #endregion

        #region Public proporties
        public int Age
        {
            get { return _age; }
        }
        /// <summary>
        /// True if unemployed primo
        /// </summary>
        public bool Unemployed
        {
            get { return _w == 0.0; }
        }

        /// <summary>
        /// Duration of unemployment spell
        /// </summary>
        public int UnemploymentDuration
        {
            get { return _unempDuration; }
        }
        public double Wealth
        {
            get { return _wealth; }
        }
        public double Income
        {
            get { return _income; }
        }

        public int No
        {
            get { return _no; }
        }

        public int Ok
        {
            get { return _ok; }
        }

        public double Productivity
        {
            get { return _productivity; }
        }
        public double YearConsumption
        {
            get { return _yr_consumption; }
        }
        public int YearEmployment
        {
            get { return _yr_employment; }
        }

        public double CES_Price
        {
            get { return _P_CES; }
        }

        public Firm FirmEmployment
        {
            get { return _firmEmployment; }
        }

        public double ConsumptionBudget
        {
            get { return _consumption_budget; }
        }

        public double ConsumptionValue
        {
            get { return _consumption_value; }
        }

        #endregion

    }
}
