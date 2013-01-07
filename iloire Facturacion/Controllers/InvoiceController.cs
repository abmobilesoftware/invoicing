/*
	Iván Loire - www.iloire.com
	Please readme README file for license terms.

	ASP.NET MVC3 ACME Invocing app (demo app for training purposes)
*/
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MvcPaging;
using System.Globalization;
using Rotativa;
using SmsFeedback_EFModels;
using System.Data.Entity.Validation;
using System.Diagnostics;

namespace iloire_Facturacion.Controllers
{
    [Authorize]
   public class InvoiceController : BaseController
    {
       public static string cSuccess = "success";

        private InvoiceDB db = new InvoiceDB();
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private int defaultPageSize = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DefaultPaginationSize"]);

        /*CUSTOM*/

        private void FillIndexViewBags(IPagedList<Invoice> invoices)
        {
            ViewBag.NetTotal = invoices.Sum(i => i.NetTotal);
            ViewBag.TotalWithVAT = invoices.Sum(i => i.TotalWithVAT);
            ViewBag.AdvancePaymentTaxAmountTotal = invoices.Sum(i => i.AdvancePaymentTaxAmount);
        }

        public ViewResultBase Search(string text, string from, string to, int? page, int? pagesize, bool? proposal = false, bool? reminder = false)
        {
            Session["invoiceText"] = text;
            Session["invoiceFrom"] = from;
            Session["invoiceTo"] = to;

            var invoices = db.Invoices.Include(i => i.InvoiceDetails).Include(i => i.Company);

            if (!string.IsNullOrWhiteSpace(from))
            {
                DateTime fromDate;
                if (DateTime.TryParse(from, CultureInfo.CurrentUICulture, DateTimeStyles.AssumeLocal, out fromDate))
                    invoices = invoices.Where(t => t.DateCreated >= fromDate);
            }
            if (!string.IsNullOrWhiteSpace(to)) 
            {
                DateTime toDate;
                if (DateTime.TryParse(to, CultureInfo.CurrentUICulture, DateTimeStyles.AssumeLocal, out toDate))
                   invoices = invoices.Where(t => t.DateCreated <= toDate);
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                invoices = invoices.Where(t => (t.Notes.ToLower().IndexOf(text.ToLower()) > -1) 
                    || (t.Name.ToLower().IndexOf(text.ToLower()) > -1)
                    || (t.Company.Name.ToLower().IndexOf(text.ToLower()) > -1)
                );
            }

            ViewBag.IsProposal = proposal;
            ViewBag.IsReminder = reminder;

            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;

            IPagedList<Invoice> invoices_paged = null;

            if (proposal == true)//once the data is in memory, i can filter by IsProposal
                invoices = invoices.Where(i => i.InvoiceNumber == 0);  //we can not use  Where(i => i.IsProposal) from within the LINQ db context                
            else
                invoices = invoices.Where(i => i.InvoiceNumber > 0); //we can not use  Where(i => i.IsProposal) from within the LINQ db context                     

            invoices_paged = invoices.OrderByDescending(i => i.DateCreated).ToPagedList(currentPageIndex, (pagesize.HasValue) ? pagesize.Value : defaultPageSize);

            FillIndexViewBags(invoices_paged);

            if (Request.IsAjaxRequest())
                return PartialView("Index", invoices_paged);
            else
                return View("Index", invoices_paged);
        }

        public PartialViewResult UnPaidInvoices()
        {
           var invoices = db.Invoices.Include(i => i.Company).Where(i => i.Paid == false && i.DueDate >= DateTime.Now && i.InvoiceNumber > 0).OrderBy(i => i.DueDate);
            return PartialView("InvoicesListPartial", invoices.ToList());
        }

        public PartialViewResult LatestProposals() {
           var invoices = db.Invoices.Include(i => i.Company).Where(i => i.InvoiceNumber == 0).OrderByDescending(i => i.DateCreated);
            return PartialView("InvoicesListPartial", invoices.ToList());  
        }

        public PartialViewResult OverDueInvoices()
        {
           var inv = db.Invoices.ToList();
           var invoices = db.Invoices.Include(i => i.Company).Where(i => i.Paid == false && i.DueDate < DateTime.Now && i.InvoiceNumber > 0).OrderBy(i => i.DueDate);
            return PartialView("InvoicesListPartial", invoices.ToList());
        }

        
        public PartialViewResult LastInvoicesByCustomer(string id)
        {
           var invoices = db.Invoices.Include(i => i.Company).Where(i => i.Name == id && i.InvoiceNumber > 0).OrderByDescending(i => i.DateCreated);
            return PartialView("InvoicesListPartial", invoices.ToList());  
        }

        public PartialViewResult LastProposalsByCustomer(string id)
        {
           var invoices = db.Invoices.Include(i => i.Company).Where(i => i.Name == id && i.InvoiceNumber == 0).OrderByDescending(i => i.DateCreated);
            return PartialView("InvoicesListPartial", invoices.ToList());  
        }

        /*END CUSTOM*/

        //
        // GET: /Invoice/

        public ActionResult Index(string filter, int? page, int? pagesize, bool? proposal = false, bool? reminder = false)
        {
            #region remember filter stuff
            if (filter == "clear")
            {
                Session["invoiceText"] = null;
                Session["invoiceFrom"] = null;
                Session["invoiceTo"] = null;
            }
            else
            {
                if ((Session["invoiceText"] != null) || (Session["invoiceFrom"] != null) || (Session["invoiceTo"] != null))
                {
                    return RedirectToAction("Search", new { text = Session["invoiceText"], from = Session["invoiceFrom"], to = Session["invoiceTo"], proposal=proposal });
                }
            }
            #endregion
       
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            var invoices = db.Invoices.Include(i => i.InvoiceDetails).Include(i => i.Company).Include(i=> i.Company.Contact);
            ViewBag.IsProposal = proposal;
            ViewBag.IsReminder = reminder;
            
            IPagedList<Invoice> invoices_paged = null;
            if (proposal == true)
            {
                invoices = invoices.Where(i => i.InvoiceNumber == 0);  //we can not use  Where(i => i.IsProposal) from within the LINQ db context                
            }
            else
            {
                invoices = invoices.Where(i => i.InvoiceNumber > 0);  //we can not use  Where(i => i.IsProposal) from within the LINQ db context                
            }

            invoices_paged = invoices.OrderByDescending(i => i.DateCreated).ToPagedList(currentPageIndex, (pagesize.HasValue) ? pagesize.Value : defaultPageSize);

            FillIndexViewBags(invoices_paged);

            return View(invoices_paged);
        }

        //
        // GET: /Invoice/Print/5

        public ViewResult Print(int id, bool? proposal = false, bool? reminder = false)
        {
            try
            {
                if (Request["lan"] != null)
                {
                    //valid culture name?
                    CultureInfo[] cultures = System.Globalization.CultureInfo.GetCultures
                             (CultureTypes.SpecificCultures);

                    var selectCulture = from p in cultures
                                        where p.Name == Request["lan"]
                                        select p;
                
                    if (selectCulture.Count() == 1)
                        System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(Request["lan"]);
                }
            }
            catch
            {
            }
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            //System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("ro-RO");
            //System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("ro-RO");
            ViewBag.Print = true;
            ViewBag.MyCompany = System.Configuration.ConfigurationManager.AppSettings["MyCompanyName"];
            ViewBag.MyCompanyID = System.Configuration.ConfigurationManager.AppSettings["MyCompanyID"];
            ViewBag.MyVATID = System.Configuration.ConfigurationManager.AppSettings["MyVATID"];
            ViewBag.MyCompanyAddress = System.Configuration.ConfigurationManager.AppSettings["MyCompanyAddress"];
            ViewBag.MyCompanyPhone = System.Configuration.ConfigurationManager.AppSettings["MyCompanyPhone"];
            ViewBag.MyCompanyJ  = System.Configuration.ConfigurationManager.AppSettings["MyCompanyJ"];
            ViewBag.MyEmail = System.Configuration.ConfigurationManager.AppSettings["MyEmail"];
            ViewBag.VatID = System.Configuration.ConfigurationManager.AppSettings["VatID"];
            ViewBag.Bank = System.Configuration.ConfigurationManager.AppSettings["MyBank"];
            ViewBag.MyBankAccount = System.Configuration.ConfigurationManager.AppSettings["MyBankAccount"];
            ViewBag.Invoice_SwiftNumber = System.Configuration.ConfigurationManager.AppSettings["SwiftNumber"];

            Invoice invoice = db.Invoices.Find(id);
            if (proposal == true)
                return View("PrintProposal", invoice);
            else if (reminder == true)
                return View("PrintReminder", invoice);
            else
                return View(invoice);
        }

        //
        // GET: /Invoice/Pdf/5

        public ActionResult Pdf(int id, bool? proposal = false, bool? reminder = false)
        {
            return new ActionAsPdf(
                           "Print",
                           new { id = id, proposal = proposal, reminder = reminder }) { FileName = "Invoice.pdf" };
        }

        //
        // GET: /Invoice/Create

        public ActionResult Create(bool? proposal=false)
        {
            Invoice i = new Invoice();
            i.DateCreated = DateTime.Now;
            i.AutoGenerated = false;
           //DA 30 days term for paying bills
            i.DueDate = DateTime.Now.AddDays(30); //30 days after today
            //i.DueDate = DateTime.Now.AddDays(14); //14 days after today
            i.AdvancePaymentTax = Convert.ToDecimal(System.Configuration.ConfigurationManager.AppSettings["DefaultAdvancePaymentTax"]);

            if (!proposal == true)
            {
                //generate next invoice number
                var next_invoice = (from inv in db.Invoices
                                    orderby inv.InvoiceNumber descending
                                    select inv).FirstOrDefault();
                if (next_invoice != null)
                    i.InvoiceNumber = next_invoice.InvoiceNumber + 1;
            }
            ViewBag.IsProposal = proposal;
            ViewBag.CompanyName = new SelectList(db.Companies.OrderBy(c => c.Name), "Name", "Name");
            return View(i);
        }

        //
        // POST: /Invoice/Create

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(Invoice invoice, bool? proposal = false, bool? reminder = false)
        {
           ViewBag.CompanyName = new SelectList(db.Companies.OrderBy(c => c.Name), "Name", "Name", invoice.CompanyName);
            ViewBag.IsProposal = proposal;
            ViewBag.IsReminder = reminder;

            if (ModelState.IsValid)
            {
                //make sure invoice number doesn't exist
                if (proposal == false)
                {
                    var invoice_exists = (from inv in db.Invoices where inv.InvoiceNumber == invoice.InvoiceNumber select inv).FirstOrDefault();
                    if (invoice_exists != null)
                    {
                        ModelState.AddModelError("InvoiceNumber", "Invoice with that number already exists");
                        return View(invoice);
                    }
                }
                db.Invoices.Add(invoice);
                try
                {
                   db.SaveChanges();
                }
                catch (DbEntityValidationException dbEx)
                {
                   foreach (var validationErrors in dbEx.EntityValidationErrors)
                   {
                      foreach (var validationError in validationErrors.ValidationErrors)
                      {                         
                         logger.ErrorFormat("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                      }
                   }
                }
                return RedirectToAction("Edit", "Invoice", new { id = invoice.InvoiceId, proposal = proposal });  
            }
            return View(invoice);
        }
        
        //
        // GET: /Invoice/Edit/5

        public ActionResult Edit(int id, bool? proposal = false, bool? makeinvoice = false, bool? makeproposal = false, bool? reminder = false)
        {
            Invoice invoice = db.Invoices.Find(id);
            ViewBag.CompanyName = new SelectList(db.Companies.OrderBy(c => c.Name), "Name", "Name", invoice.CompanyName);

            if (makeinvoice == true)
            {
                //we want to make invoice from this proposal
                //generate next invoice number
                var next_invoice = (from inv in db.Invoices
                                    orderby inv.InvoiceNumber descending
                                    select inv).FirstOrDefault();
                
                if (next_invoice != null)
                    invoice.InvoiceNumber = next_invoice.InvoiceNumber + 1; //assign next available invoice number 

                invoice.DateCreated = DateTime.Now;
                invoice.DueDate = DateTime.Now.AddDays(30);

                ViewBag.Warning = "The current item is going to be converted on Invoice. A new InvoiceNumber has been pre-assigned. The dates will be modified accordingly. Click on 'Save' to continue.";
                ViewBag.ShowMakeInvoice = ViewBag.ShowMakeProposal = false;
            }
            else if (makeproposal == true)
            {
                invoice.InvoiceNumber = 0; //reset invoice number                
                ViewBag.Warning = "The current item is going to be converted on Proposal. You will lose InvoiceNumber. If that's what you want click on 'Save'";
                ViewBag.ShowMakeInvoice = ViewBag.ShowMakeProposal = false;
            }
            else
            {
                if (!invoice.IsProposal && proposal == true)
                {
                    //item is invoice, redirect to proper route (hack)
                    return RedirectToAction("Edit", new { id = id, proposal = false, makeinvoice = false });
                }

                ViewBag.ShowMakeInvoice = invoice.IsProposal;
                ViewBag.ShowMakeProposal = !invoice.IsProposal;
            }

            ViewBag.IsProposal = invoice.IsProposal;

            return View(invoice);
        }

        //
        // POST: /Invoice/Edit/5

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(Invoice invoice, bool? proposal = false, bool? reminder = false)
        {
           //System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
           //System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo("ro-RO");
           ViewBag.CompanyName = new SelectList(db.Companies.OrderBy(c => c.Name), "Name", "Name", invoice.CompanyName);
            ViewBag.IsProposal = proposal;
            ViewBag.IsPrReminder = reminder;
            if (ModelState.IsValid)
            {
                if (proposal == false)
                {
                    //make sure invoice number doesn't exist
                    var invoice_exists = (from inv in db.Invoices
                                          where inv.InvoiceNumber == invoice.InvoiceNumber
                                          && inv.InvoiceId != invoice.InvoiceId
                                          select inv).Count();

                    if (invoice_exists > 0)
                    {
                        ModelState.AddModelError("InvoiceNumber", "Invoice with that number already exists");
                        return View(invoice);
                    }
                }

                db.Entry(invoice).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index", new { proposal = proposal });
            }
            return View(invoice);
        }

        //
        // GET: /Invoice/Delete/5

        public ActionResult Delete(int id, bool? proposal = false, bool? reminder = false)
        {
            ViewBag.IsProposal = proposal;
            ViewBag.IsReminder = reminder;
            Invoice invoice = db.Invoices.Find(id);
            return View(invoice);
        }

        //
        // POST: /Invoice/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id, bool? proposal = false, bool? reminder = false)
        {
           ViewBag.IsProposal = proposal;
           ViewBag.IsReminder = reminder;
           Invoice invoice = db.Invoices.Find(id);
           //List<InvoiceDetails> invdetails = invoice.InvoiceDetails.ToList();
           //invdetails.ForEach(details => {              
           //   invoice.InvoiceDetails.Remove(details);});           
           db.Invoices.Remove(invoice);
           db.SaveChanges();
           return RedirectToAction("Index", new { proposal = proposal });
        }

        public ActionResult CreateInvoices()
        {
           //DA the idea is to create all invoices for companies with billing day equals with today
           var result = CreateAutoInvoices();
           return View("CreateAutoInvoices", result);
        }

        public ActionResult ResetSubscriptions()
        {           
           var res = AutoResetSubscriptions();
           return View("ResetSubscriptions", res);
        }

        private List<KeyValuePair<string,string>> AutoResetSubscriptions()
        {
           //DA make sure that we have a invoice generated, otherwise we cannot reset the db
           int today = DateTime.Now.Day;           
           var billedToday = (from cp in db.Companies where cp.SubscriptionDetail.BillingDay == today select cp).ToList();
           var result = new List<KeyValuePair<string, string>>();
           foreach (Company company in billedToday)
           {
              var lastInvoice = (from inv in company.Invoices where inv.AutoGenerated == true orderby inv.DateCreated descending select inv).FirstOrDefault();
              KeyValuePair<string, string> resultSoFar = new KeyValuePair<string,string>(company.Name,cSuccess);
              var canProceed = false;
              if (lastInvoice == null)
              {
                 resultSoFar = GenerateAutoInvoiceForCompany(company);
                 canProceed = resultSoFar.Value == cSuccess;                 
              }  
              else {
                 if (lastInvoice.DateCreated.Date < DateTime.Now.Date)
                 {
                    resultSoFar = GenerateAutoInvoiceForCompany(company);
                    canProceed = resultSoFar.Value == cSuccess;  
                 }
                 else {
                     //the only remaining option is that the autoInvoice was created today -> we're ok
                    canProceed = true;
                 }                 
              }
              if (canProceed)
              {
                 //we got here -> for sure we have an auto-generated invoice so we can safely reset the subscription details
                 /**
                  * Reset procedure:
                  * 1. If ExtraCredit was bought this month (or left from the previous month), update the remaining Credit
                  * 2. Update the RemainingSms
                  * 3. Update the SpentAmount
                  * 4. Update the ExtraCreditThisMonth
                  */
                 company.SubscriptionDetail.RemainingCreditFromPreviousMonth = company.SubscriptionDetail.RemainingExtraCreditForNextMonth;
                 company.SubscriptionDetail.RemainingSMS = company.SubscriptionDetail.SubscriptionSMS;
                 company.SubscriptionDetail.SpentThisMonth = 0;
                 company.SubscriptionDetail.ExtraAddedCreditThisMonth = 0;
                 try
                 {
                    db.SaveChanges();
                    result.Add(resultSoFar);
                 }
                 catch (Exception ex)
                 {
                    result.Add(new KeyValuePair<string, string>(company.Name, ex.Message));
                 }
              }
              else
              {
                 //DA for some reason we cannot proceed :(
                 result.Add(new KeyValuePair<string, string>(company.Name, "For some reason we cannot reset the subscription for this company. Do check the configuration"));
              }
           }
           return result;
        }

        private List<KeyValuePair<string,string>> CreateAutoInvoices()
        {
           var result = new List<KeyValuePair<string, string>>();
           int today = DateTime.Now.Day;
           var billedToday = (from cp in db.Companies where cp.SubscriptionDetail.BillingDay == today select cp).ToList();
           foreach (Company company in billedToday)
           {
              var res = GenerateAutoInvoiceForCompany(company);
              result.Add(res);
           }
           return result;
        }

        private KeyValuePair<string,string> GenerateAutoInvoiceForCompany(Company company)
        {
          
           Invoice invoice = new Invoice();
           invoice.DateCreated = DateTime.Now;
           invoice.AutoGenerated = true;
           //DA 30 days term for paying bills
           invoice.DueDate = DateTime.Now.AddDays(30); //30 days after today            

           invoice.AdvancePaymentTax = 0; //DA nothing paid in advance
           //generate next invoice number
           var next_invoice = (from inv in db.Invoices
                               orderby inv.InvoiceNumber descending
                               select inv).FirstOrDefault();
           if (next_invoice != null)
           {
              invoice.InvoiceNumber = next_invoice.InvoiceNumber + 1;
           }
           invoice.Paid = false;
           invoice.Notes = String.Format(System.Configuration.ConfigurationManager.AppSettings["NotesForAutogeneratedInvoice"], DateTime.Now.ToLongDateString());
           invoice.ProposalDetails = System.Configuration.ConfigurationManager.AppSettings["ProposalDetailsForAutogeneratedInvoice"];
           invoice.CompanyName = company.Name;
           invoice.Currency = company.SubscriptionDetail.DefaultCurrency;
           db.Invoices.Add(invoice);
           //DA add the subscription costs
           InvoiceDetailsTemplate mthSubscriptionDetails = company.SubscriptionDetail.MonthlySubscriptionTemplate;
           if (mthSubscriptionDetails != null)
           {
              AddArticleToInvoice(company, invoice, mthSubscriptionDetails, mthSubscriptionDetails.Price);
           }
           //DA add the extra SMS costs
           InvoiceDetailsTemplate extraCosts = company.SubscriptionDetail.ExtraSMSCostsDetails;
           bool extraCostsExist = (company.SubscriptionDetail.AmountToBeBilledFor > (decimal)0.01);
           if (extraCosts != null && extraCostsExist)
           {
              AddArticleToInvoice(company, invoice, extraCosts, company.SubscriptionDetail.AmountToBeBilledFor);
           }
           //DA the bill for the extra credit purchased should be a separate one
           try
           {
              db.SaveChanges();
           }
           catch (DbEntityValidationException dbEx)
           {
              foreach (var validationErrors in dbEx.EntityValidationErrors)
              {
                 foreach (var validationError in validationErrors.ValidationErrors)
                 {                    
                    logger.ErrorFormat("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                 }
              }
              //TODO log exception
              return new KeyValuePair<string, string>(company.Name, dbEx.Message);
           }
           catch (Exception ex)
           {
              logger.Error("GenerateAutoInvoice SaveChanges", ex);
              return new KeyValuePair<string, string>(company.Name, ex.Message);
           }
           //no issues encountered
           return new KeyValuePair<string, string>(company.Name, cSuccess);
        }

        private void AddArticleToInvoice(Company company, Invoice invoice, InvoiceDetailsTemplate template, Decimal price)
        {
           InvoiceDetails det = new InvoiceDetails();
           det.InvoiceInvoiceId = invoice.InvoiceId;
           det.Invoice = invoice;
           det.Quantity = template.Quantity;
           det.Price = price;
           det.VAT = template.VAT;
           det.DateCreated = DateTime.Now;
           det.Article = template.Article;
           db.InvoiceDetails.Add(det);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}