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
using SmsFeedback_EFModels;
using System.Data.Entity.Validation;
using System.Diagnostics;

namespace iloire_Facturacion.Controllers
{
   [Authorize]
   public class CustomerController : BaseController
    {
        private int defaultPageSize = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["DefaultPaginationSize"]);
        private InvoiceDB db = new InvoiceDB();

        /*CUSTOM*/
        public ViewResultBase Search(string q, int? page)
        {
            IQueryable<Company> customers=db.Companies;

            if (q.Length == 1)//alphabetical search, first letter
            {
                ViewBag.LetraAlfabetica = q;
                customers =  customers.Where (c=>c.Name.StartsWith(q));
            }
            else if (q.Length>1){ 
                //normal search
                customers = customers.Where(c => c.Name.IndexOf(q) > -1);
            }

            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            var customersListPaged = customers.OrderBy(i => i.Name).ToPagedList(currentPageIndex, defaultPageSize);
            
            if (Request.IsAjaxRequest())
                return PartialView("Index", customersListPaged);
            else
                return View("Index", customersListPaged);
        }

        /*END CUSTOM*/
        
        
        //
        // GET: /Customer/

        public ViewResult Index(int? page)
        {
            int currentPageIndex = page.HasValue ? page.Value - 1 : 0;
            return View(db.Companies.Include(i => i.Contact).Include(i => i.SubscriptionDetail).OrderBy(c => c.Name).ToPagedList(currentPageIndex, defaultPageSize));
        }

        //
        // GET: /Customer/Details/5

        public ViewResult Details(string id)
        {
           Company customer = db.Companies.Find(id);
            return View(customer);
        }

        //
        // GET: /Customer/Create

        public ActionResult Create()
        {
            return PartialView();
        } 

        //
        // POST: /Customer/Create

        [HttpPost]
        public ActionResult Create(Company customer)
        {
           if (ModelState.IsValid)
           {
              //DA - billing day should be given
              //DA we need a valid subscription type              
              SubscriptionDetail lsDetails = new SubscriptionDetail() {BillingDay=1, DefaultCurrency ="EUR",RemainingAmount=10,RemainingSMS=200,SpendingLimit=10,SubscriptionSMS=200,WarningLimit=1 };              
              customer.SubscriptionDetail = lsDetails;
              Contact lContact = db.Contacts.Find(1);
              lsDetails.PrimaryContact = lContact;
              lsDetails.SecondaryContact = lContact;
              customer.Contact = lContact;
             
              db.Companies.Add(customer);
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
                       Trace.TraceInformation("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                    }
                 }
              }
              //return list of customers as it is ajax request
              return PartialView("CustomerListPartial", db.Companies.OrderBy(c => c.Name).ToPagedList(0, defaultPageSize));
              //return RedirectToAction("Index");  
           }
           this.Response.StatusCode = 400;
           return PartialView(customer);
        }
        
        //
        // GET: /Customer/Edit/5
 
        public ActionResult Edit(string id)
        {
           Company customer = db.Companies.Find(id);
           db.Entry(customer).Reference(u => u.SubscriptionDetail).Load();
           db.Entry(customer).Reference(u => u.Contact).Load();
           return PartialView(customer);
        }

        //
        // POST: /Customer/Edit/5

        [HttpPost]
        public ActionResult Edit(Company customer)
        {
            if (ModelState.IsValid)
            {
                db.Entry(customer).State = EntityState.Modified;
                db.SaveChanges();
                //return RedirectToAction("Index");
                //return list of customers as it is ajax request
                return PartialView("CustomerListPartial", db.Companies.Include(i => i.Contact).Include(i => i.SubscriptionDetail).OrderBy(c => c.Name).ToPagedList(0, defaultPageSize));
            }
            this.Response.StatusCode = 400;
            return PartialView(customer);
        }

        //
        // GET: /Customer/Delete/5
 
        public ActionResult Delete(string id)
        {
           Company customer = db.Companies.Find(id);
            return View(customer);
        }

        //
        // POST: /Customer/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(string id)
        {
           Company customer = db.Companies.Find(id);
           db.Companies.Remove(customer);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}