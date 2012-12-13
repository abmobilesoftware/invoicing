﻿using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Data.Objects;
using SmsFeedback_EFModels;

public class InvoiceDB : smsfeedbackEntities {

    public InvoiceDB()
    {}

    //public DbSet<Customer> Customers {get; set;}
    public DbSet<Provider> Providers { get; set; }
    //public DbSet<InvoiceVM> Invoices
    //{
    //   get;
    //   set;
    //}
    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<PurchaseType> PurchaseTypes { get; set; }
    //public DbSet<InvoiceDetailsVM> InvoiceDetails { get; set; }

    //public DbSet<User> Users { get; set; }
}