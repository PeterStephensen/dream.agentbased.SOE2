using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dream.Models.SOE_Basic
{
    #region enum ECommunicate
    public enum ECommunicate
    {
        Yes,
        No,
        Ok,
        ThankYou,
        YouAreFired,
        //YouAreHiredInStartup,
        //YouAreCustomerInStartup,
        AvertiseGood,
        AvertiseJob,
        JobApplication,
        IQuit,
        Death,
        CanIBuy,
        Inheritance,
        Statistics,
        Initialize  // Only used during initialization
    }
    #endregion

    #region EStatistics
    public enum EStatistics
    {
        FirmCloseNatural,
        FirmCloseTooBig,
        FirmCloseNegativeProfit,
        FirmCloseZeroEmployment,
        FirmNew,
        Death,
        Profit,
        CouldNotFindSupplier,
        ChangeShopInSearchForShop,
        ChangeShopInBuyFromShopNull,
        ChangeShopInBuyFromShopLookingForGoods,
        CouldNotFindFirmWithGoods,
        SuccesfullTrade,
        SuccesfullTradeNonZero,
        ZeroBudget,
        BuyFromShop
    }
    #endregion

    #region Message Class
    public class Message
    {

        #region Public fields
        public ECommunicate ComID;
        public object Object;
        #endregion

    }
    #endregion

    #region EShock
    public enum EShock
    {
        Nothing,
        Productivity,
        Tsunami,
        ProductivitySector0,
        LaborSupply
    }

    #endregion

}