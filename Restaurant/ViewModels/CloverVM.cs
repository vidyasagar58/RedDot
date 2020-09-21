﻿using com.clover.remotepay.sdk;
using com.clover.sdk.v3.payments;
using com.clover.remotepay.transport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Input;
using System.Windows;
using NLog;
using RedDot.Models;

namespace RedDot
{
    public class CloverVM:INPCBase
    {


        private static Logger logger = LogManager.GetCurrentClassLogger();
        public ICommand CreditAuthClicked { get; set; }
        public ICommand CreditSaleClicked { get; set; }
        public ICommand LineItemClicked { get; set; }
        public ICommand CreditRefundClicked { get; set; }
        public ICommand VoidClicked { get; set; }
        public ICommand CommandClicked { get; set; }
     

        private bool enabledebitsale = GlobalSettings.Instance.EnableDebitSale;
        private bool enablecreditsale = GlobalSettings.Instance.EnableCreditSale;
        private bool enableauthcapture = GlobalSettings.Instance.EnableAuthCapture;


        ICloverConnector cloverConnector;
        CloverListener ccl;





        private string m_transtype = "";
        private Payment m_payment;

        private SalesModel m_salesmodel { get; set; }
        private VFD vfd;


        //private bool canExecute = true;
        private Window m_parent;
        private SecurityModel m_security;
        private string _reason;

        public CloverVM(Window parent,SecurityModel security, Ticket m_ticket, string transtype,Payment payment,string reason)
        {

            CurrentTicket = m_ticket;
            m_parent = parent;
            m_transtype = transtype;
            m_payment = payment;
            m_security = security;
            _reason = reason;

            if (transtype.ToUpper() == "VOID") NumPadVisibility = Visibility.Hidden; else NumPadVisibility = Visibility.Visible;



            NotifyPropertyChanged("BalanceStr");



           CreditAuthClicked = new RelayCommand(ExecuteCreditAuthClicked, param => this.CanExecuteAuthCaptureClicked);
            CreditSaleClicked = new RelayCommand(ExecuteCreditSaleClicked, param => this.CanExecuteCreditSaleClicked);
         

           LineItemClicked = new RelayCommand(ExecuteLineItemClicked, param => true);

           CreditRefundClicked = new RelayCommand(ExecuteCreditRefundClicked, param => true);
         
            VoidClicked = new RelayCommand(ExecuteVoidClicked, param => this.CanExecuteVoidClicked);
            CommandClicked = new RelayCommand(ExecuteCommandClicked, param => true);

            m_salesmodel = new SalesModel(m_security);
            vfd = new VFD(GlobalSettings.Instance.DisplayComPort);


            //initialize device
            cloverConnector = GlobalSettings.Instance.cloverConnector;
            ccl = GlobalSettings.Instance.ccl;

            if (!ccl.deviceReady)
            {
                cloverConnector.InitializeConnection();
            }


        }



        public Ticket CurrentTicket { get; set; }
        public string BalanceStr
        {
            get
            {
                return String.Format("{0:0.00}", CurrentTicket.Balance);
            }
        }


        public Visibility NumPadVisibility { get; set; }

        private string m_refundamount;
        public string RefundAmount
        {
            get { return m_refundamount; }
            set
            {
                m_refundamount = value;
                NotifyPropertyChanged("RefundAmount");
            }
        }

        string m_posmessage;
        public string POSMessage
        {
            get { return m_posmessage; }
            set
            {
                m_posmessage = value;
                NotifyPropertyChanged("POSMessage");

            }
        }

        string m_posxml;
        public string POSXML
        {
            get { return m_posxml; }
            set
            {
                m_posxml = value;
                NotifyPropertyChanged("POSXML");

            }
        }

        public bool CanExecuteChargeClicked
        {
            get
            {
                if (!ccl.deviceReady) return false;

                return (m_transtype.ToUpper() == "SALE");
            }

        }

        public bool CanExecuteDebitSaleClicked
        {
            get
            {
                if (!ccl.deviceReady) return false;

                if (CanExecuteChargeClicked)
                {
                    return enabledebitsale;
                }
                else
                    return false;
            }

        }


        public bool CanExecuteCreditSaleClicked
        {
            get
            {
                if (!ccl.deviceReady) return false;

                if (CanExecuteChargeClicked)
                {
                    return enablecreditsale;
                }
                else
                    return false;
            }

        }


        public bool CanExecuteAuthCaptureClicked
        {
            get
            {
                if (!ccl.deviceReady) return false;

                if (CanExecuteChargeClicked)
                {
                    return enableauthcapture;
                }
                else
                    return false;
            }

        }



        public bool CanExecuteVoidClicked
        {
            get
            {
                if (!ccl.deviceReady) return false;

                return (m_transtype.ToUpper() == "VOID");
            }

        }


        
        public void ExecuteCreditAuthClicked(object amount)
        {
            string temp = amount as string;
            decimal amt;

            try
            {



                if (temp != "")
                {
                    amt = decimal.Parse(temp);
                    if (amt > CurrentTicket.Balance)
                    {
                        TouchMessageBox.Show("Amount is greater than balance!!!");
                        return;
                    }


                    logger.Info("COMMAND:Credit Authorize , Amount=" + amt);


                    var authorize = new AuthRequest();
                    authorize.ExternalId = ExternalIDUtil.GenerateRandomString(13);
                    authorize.Amount = (int)(amt * 100);
                    authorize.AutoAcceptSignature = true;
                    authorize.AutoAcceptPaymentConfirmations = true;
                    authorize.DisableDuplicateChecking = !GlobalSettings.Instance.AllowDuplicates;
                    authorize.DisableCashback = !GlobalSettings.Instance.AllowCashBack;
                 

                    if (GlobalSettings.Instance.AuthSigOnPaper)
                        authorize.SignatureEntryLocation = DataEntryLocation.ON_PAPER;  //authorization is always on paper so customer can enter tip
                    else
                        authorize.SignatureEntryLocation = GlobalSettings.Instance.SignatureOnScreen ? DataEntryLocation.ON_SCREEN : DataEntryLocation.ON_PAPER;

                    ccl.saleDone = false;
                    cloverConnector.Auth(authorize);


                    while (!ccl.saleDone)
                    {
                        Thread.Sleep(1000);
                    }


                    logger.Info("Credit Sale:Order ID:" + ccl.orderId);
                    logger.Info("Credit Sale:Payment ID=" + ccl.paymentId);
                    logger.Info("Credit Sale:response =" + ccl.result.ToString());

                    POSMessage = "Response:" + ccl.message + (char)13 + (char)10;
                    POSMessage = "Response:" + ccl.result.ToString() + (char)13 + (char)10;
                    POSMessage = "Response:PaymentID=" + ccl.paymentId + (char)13 + (char)10;



                    if (ccl.result == ResponseCode.SUCCESS)
                    {
                        decimal authamt = (decimal)ccl.Payment.amount;

                        if (authamt >= amt)
                        {
                            //full approval 
                            bool result = AddCreditPayment(amt, ccl.Payment, DateTime.Now,"");

                            if (result == false)
                            {
                                TouchMessageBox.Show("Payment record insert failed.");
                                return;
                            }


                        }
                        else
                        {
                            //partial approval
                            bool result = AddCreditPayment(authamt, ccl.Payment, DateTime.Now,"");

                            if (result == false)
                            {
                                TouchMessageBox.Show("Payment record insert failed.");
                                return;
                            }
                        }


                    
                        //print credit slip
                  
                            if (ccl.Payment.cardTransaction.referenceId != null)
                            {
                                Payment payment = m_salesmodel.GetPayment(ccl.Payment.cardTransaction.referenceId);
                                if (payment != null)   ReceiptPrinterModel.AutoPrintCreditSlip( payment);
                            }
                            else TouchMessageBox.Show("Reference ID is NULL -- can not locate receipt.");

                      

                        m_parent.Close();

                    }
                    else
                    {
                        logger.Error(ccl.message);
                        TouchMessageBox.Show("SALE TRANSACTION FAILED !!!! ERROR:  " + ccl.message);

                    }


                }
                else TouchMessageBox.Show("Please Enter $ Amt to Process.");

            }
            catch (Exception e)
            {

                TouchMessageBox.Show("Charge Clicked: " + e.Message);
            }


        }



        



        public void ExecuteCreditSaleClicked(object amount)
        {
            string temp = amount as string;
            decimal amt;

            try
            {



                if (temp != "")
                {
                    amt = decimal.Parse(temp);
                    if (amt > CurrentTicket.Balance)
                    {
                        TouchMessageBox.Show("Amount is greater than balance!!!");
                        return;
                    }


                    logger.Info("COMMAND:Credit Sale , Amount=" + amt);


                    var pendingSale = new SaleRequest();
                    pendingSale.ExternalId = ExternalIDUtil.GenerateRandomString(13);
                    pendingSale.Amount = (int) (amt * 100);

                    pendingSale.AutoAcceptSignature = true;
                    pendingSale.AutoAcceptPaymentConfirmations = true;
                    pendingSale.DisableDuplicateChecking = !GlobalSettings.Instance.AllowDuplicates;
                    pendingSale.SignatureEntryLocation = GlobalSettings.Instance.SignatureOnScreen ? DataEntryLocation.ON_SCREEN : DataEntryLocation.ON_PAPER;
                    pendingSale.DisableCashback = !GlobalSettings.Instance.AllowCashBack;
                

                    ccl.saleDone = false;
                    cloverConnector.Sale(pendingSale);
                    

                    while (!ccl.saleDone)
                    {
                        Thread.Sleep(1000);
                    }

                    logger.Info("Credit Sale:Order ID:" + ccl.orderId);
                    logger.Info("Credit Sale:Payment ID=" + ccl.paymentId);
                    logger.Info("Credit Sale:response =" + ccl.result.ToString());

                    POSMessage = "Response:" + ccl.message + (char)13 + (char)10;
                    POSMessage = "Response:" + ccl.result.ToString() + (char)13 + (char)10;
                    POSMessage = "Response:" + ccl.paymentId + (char)13 + (char)10;


                    if (ccl.result == ResponseCode.SUCCESS)
                    {
                        decimal authamt = (decimal)ccl.Payment.amount/100;

                        if (ccl.Payment.tipAmount/100 > GlobalSettings.Instance.TipAlertAmount)
                        {
                            TouchMessageBox.Show("Alert!!! TIP AMOUNT: " + ccl.Payment.tipAmount.ToString() + (char)13 + (char)10 + " Please verify with customer!!");
                        }

                        if (authamt >= amt)
                        {
                            //full approval 
                            bool result = AddCreditPayment(amt, ccl.Payment, DateTime.Now,"");

                            if (result == false)
                            {
                                TouchMessageBox.Show("Payment record insert failed.");
                                return;
                            }


                        }
                        else
                        {
                            //partial approval
                            bool result = AddCreditPayment(authamt, ccl.Payment, DateTime.Now,"");

                            if (result == false)
                            {
                                TouchMessageBox.Show("Payment record insert failed.");
                                return;
                            }
                        }


                        //print credit slip
                   
                            if (ccl.Payment.cardTransaction.referenceId != null)
                            {
                                Payment payment = m_salesmodel.GetPayment(ccl.Payment.cardTransaction.referenceId);
                                if (payment != null)
                                    ReceiptPrinterModel.AutoPrintCreditSlip( payment);
                            }
                            else TouchMessageBox.Show("Reference ID is NULL -- can not locate receipt.");
                           
                      


                 

                        m_parent.Close();

                    }
                    else
                    {
                        logger.Error(ccl.reason);
                        TouchMessageBox.Show("SALE TRANSACTION FAILED !!!! ERROR:  " + ccl.reason);

                    }


                }
                else TouchMessageBox.Show("Please Enter $ Amt to Process.");

            }
            catch (Exception e)
            {

                TouchMessageBox.Show("Charge Clicked: " + e.Message);
            }


        }


  

   

    public void ExecuteCreditRefundClicked(object amount)
    {
         string temp = amount as string;
         decimal amt;

         try
         {



             if (temp != "")
             {
                 amt = decimal.Parse(temp);

                 decimal originalTicketAmount = CurrentTicket.Payments.Sum(x => x.NetAmount);


                 if (amt > originalTicketAmount)
                 {
                     TouchMessageBox.Show("Amount is greater than original ticket amount!!!");
                     return;
                 }

                    TextPad tp = new TextPad("Enter Refund Reason", "");
                    Utility.OpenModal(m_parent, tp);

                    if (tp.ReturnText == "") return;



                    logger.Info("COMMAND:Refund , Amount=" + amt);


                    ManualRefundRequest request = new ManualRefundRequest();
                    request.ExternalId = ExternalIDUtil.GenerateRandomString(32);
                    //request.PaymentId = payment.PaymentID;

                    // request.OrderId = payment.OrderID;
                    request.Amount = (long)(amt*100);
                    //request.FullRefund = false;

                    ccl.refundDone = false;
                    cloverConnector.ManualRefund(request);
               
             

                    request.CardEntryMethods =0;

                    while (!ccl.refundDone)
                    {
                        Thread.Sleep(1000);
                    }

                    logger.Info("Credit Refund:Order ID:" + ccl.orderId);
                    logger.Info("Credit Refund:Payment ID=" + ccl.paymentId);
                    logger.Info("Credit Refund:response =" + ccl.result.ToString());

                    POSMessage = "Response:" + ccl.message + (char)13 + (char)10;
                    POSMessage = "Response:" + ccl.result.ToString() + (char)13 + (char)10;
                    POSMessage = "Response:" + ccl.paymentId + (char)13 + (char)10;


                    if (ccl.result == ResponseCode.SUCCESS)
                    {
                        decimal authamt = (decimal)ccl.ManualRefund.amount/100;

                     
                            //full approval 
                            bool result = AddCreditRefund(authamt, ccl.ManualRefund, DateTime.Now, "CREDIT", ccl.paymentId, ccl.orderId,tp.ReturnText);

                            if (result == false)
                            {
                                TouchMessageBox.Show("Refund record insert failed.");
                                return;
                            }




                        //print credit slip
                    
                            if (ccl.ManualRefund.cardTransaction.referenceId != null)
                            {
                                Payment payment = m_salesmodel.GetPayment(ccl.ManualRefund.cardTransaction.referenceId);
                                if (payment != null) ReceiptPrinterModel.AutoPrintCreditSlip( payment);
                            }
                            else TouchMessageBox.Show("Reference ID is NULL -- can not locate receipt.");

                      


                        m_parent.Close();

                    }
                    else
                    {
                        logger.Error(ccl.reason);
                        TouchMessageBox.Show("SALE TRANSACTION FAILED !!!! ERROR:  " + ccl.reason);

                    }





                }
             else TouchMessageBox.Show("Please Enter Refund Amount");

         }
         catch (Exception e)
         {

             TouchMessageBox.Show("Refund Clicked: " + e.Message);
         }
    }



     

    
        
       
    public void ExecuteLineItemClicked(object iditemtype)
  {

        try
        {

          decimal total = 0;
          decimal producttotal = 0;

     
          decimal taxtotal = 0;

          decimal taxabletotal = 0;

          foreach (Seat seat in CurrentTicket.Seats)
              foreach (LineItem line in seat.LineItems)
              {
                  if (line.Selected)
                  {
                     
                        producttotal = producttotal + line.AdjustedPrice * line.Quantity;
                    


                      //doesn't matter if its a service or product or giftcard , only cares if it's marked as taxable
                      if (line.Taxable)
                          taxabletotal = taxabletotal + line.AdjustedPrice * line.Quantity;
                  }

              }



              taxtotal = Math.Round(taxabletotal * GlobalSettings.Instance.SalesTaxPercent / 100, 2);

              total =  producttotal + taxtotal;

              RefundAmount = total.ToString();

        }
        catch (Exception e)
        {

              TouchMessageBox.Show("Execute Line Item Clicked: " + e.Message);
        }
  }

        

    

    public void ExecuteVoidClicked(object obj)
    {
           if (m_payment == null)
           {
               TouchMessageBox.Show("No Transaction #  to Void  with !!!");
               return;
           }

            if (m_payment.CloverPaymentId == null || m_payment.CloverOrderId == null)
            {
                TouchMessageBox.Show("No Transaction #  to Void  with !!!");
                return;
            }



            //  Payment payment = CurrentTicket.Payments.Where(x => x.ResponseId == m_transactionid).First();

            logger.Info("COMMAND:Credit Void , CloverPaymentID=" + m_payment.CloverPaymentId + ", Orderid =" + m_payment.CloverOrderId);
          // TerminalResponse resp = _device.CreditVoid(1).WithTransactionId(m_transactionid).Execute();
            VoidPaymentRequest request = new VoidPaymentRequest();

            ccl.voidDone = false;
            request.PaymentId = m_payment.CloverPaymentId;
           // request.EmployeeId = CurrentTicket.EmployeeID.ToString();
            request.OrderId = m_payment.CloverOrderId;
            request.VoidReason = "USER_CANCEL";

            cloverConnector.VoidPayment(request);

            while (!ccl.voidDone)
            {
                Thread.Sleep(1000);
            }

            AuditModel.WriteLog(m_security.CurrentEmployee.DisplayName, "Void Payment", _reason, "payment", m_payment.ID);

            logger.Info("Credit Void:RECEIVED:" + ccl.message);
           logger.Info("Credit Void:paymentid=" + ccl.paymentId);

           POSMessage = "Response:" + ccl.result + (char)13 + (char)10;


            if (ccl.result == ResponseCode.SUCCESS)
            {

               //update payment

               SalesModel.VoidCreditPayment(m_payment.CloverPaymentId, m_payment.CloverOrderId);

         
                   //force ticket to load payment into object so it will contain the tip
                
                   CurrentTicket.LoadPayment();

                Payment payment = m_salesmodel.GetPayment(m_payment.ID);

                //print credit slip

                if (payment != null)
                    {
                        ReceiptPrinterModel.AutoPrintCreditSlip( payment);
                      
                    }





                m_parent.Close();

           }
           else
           {



               logger.Error(ccl.reason);
               TouchMessageBox.Show("VOID TRANSACTION FAILED !!!! ERROR:  " + ccl.reason);


           }


    }



            public void ExecuteCommandClicked(object obj)
            {
            string command = "";

            if (obj != null) command = obj.ToString();

            switch(command)
            {
                case "Reset":

                    if (!ccl.deviceReady)
                    {
                        cloverConnector.InitializeConnection();
                    }
                   


                    ccl.resetDone = false;
                    logger.Info("COMMAND:Reset");
                    cloverConnector.ResetDevice();

                    int retries = 0;

                    while (!ccl.resetDone && retries < 30)
                    {
                        Thread.Sleep(1000);
                        retries++;
                        if (retries == 30)
                        {
                            TouchMessageBox.ShowSmall("Command Timed Out!!!");

                        }
                    }


                    POSMessage = "Response:" + ccl.message + (char)13 + (char)10;

                    logger.Debug("Reset:" + ccl.message);

                    break;

                case "Back":
                  
                    m_parent.Close();


                    break;

            }

            }






        public bool AddCreditPayment(decimal requested_amount, com.clover.sdk.v3.payments.Payment payment, DateTime paymentdate,string reason)
        {

            try
            {



                if (CurrentTicket == null)
                    return SalesModel.InsertCreditPayment(0, requested_amount, payment, paymentdate, reason);
                else
                {
                   
                    var result = SalesModel.InsertCreditPayment(CurrentTicket.SalesID, requested_amount, payment, paymentdate,reason);
                    CurrentTicket.LoadPayment(); // refresh payments to get correct balance
                    GlobalSettings.CustomerDisplay.WriteDisplay(payment.cardTransaction.cardType.ToString() + ":", requested_amount, "Balance:", CurrentTicket.Balance);
                    return result;
                }



            }
            catch (Exception e)
            {
                TouchMessageBox.Show("AddPayment:" + e.Message);
                return false;
            }
        }

        public bool AddCreditRefund(decimal requested_amount, com.clover.sdk.v3.payments.Credit refund, DateTime refunddate, string cardgroup, string paymentid, string orderid,string reason)
        {

            try
            {

                if (CurrentTicket == null)
                    return SalesModel.InsertCreditRefund(0, requested_amount, refund, refunddate, cardgroup,paymentid, orderid,reason);
                else
                {
                   
                    var result = SalesModel.InsertCreditRefund(CurrentTicket.SalesID, requested_amount, refund, refunddate, cardgroup, paymentid, orderid,reason);
                    CurrentTicket.LoadPayment();
                    GlobalSettings.CustomerDisplay.WriteDisplay("Refund:", requested_amount, "Balance:", CurrentTicket.Balance);
                    return result;
                }

            }
            catch (Exception e)
            {
                TouchMessageBox.Show("Add Refund:" + e.Message);
                return false;
            }
        }


      
        
      

    }
}
