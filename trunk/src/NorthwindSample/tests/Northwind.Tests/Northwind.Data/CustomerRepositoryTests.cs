﻿using NUnit.Framework;
using SharpArch.Core.PersistenceSupport;
using Northwind.Core;
using Northwind.Data;
using System.Diagnostics;
using SharpArch.Core;
using NUnit.Framework.SyntaxHelpers;
using Northwind.Core.DataInterfaces;
using SharpArch.Data.NHibernate;
using System.Collections.Generic;
using System;
using SharpArch.Testing.NUnit.NHibernate;

namespace Tests.Northwind.Data
{
    [TestFixture]
    [Category("DB Tests")]
    public class CustomerRepositoryTests : RepositoryTestsBase
    {
        protected override void LoadTestData() {
            CreatePersistedCustomer("WOWEE", "Jack Johnson", "Rancho pequeno", "Brazil");
            CreatePersistedCustomer("ABC12", "Jim Wirwille", "Homebrew", null);

            Customer customer = CreatePersistedCustomer("RANCH", "John Wayne", "Rancho grande", "Brazil");
            CreatePersistentOrder(customer, DateTime.Now);
            CreatePersistentOrder(customer, new DateTime(1998, 1, 13));
            CreatePersistentOrder(customer, new DateTime(2006, 2, 23));
        }

        [Test]
        public void CanGetAllCustomers() {
            IList<Customer> customers = customerRepository.GetAll();

            Assert.That(customers, Is.Not.Null);
            Assert.That(customers.Count, Is.EqualTo(3));
        }

        [Test]
        public void CanGetCustomerById() {
            Customer customer = GetCustomerById();

            Assert.That(customer.CompanyName, Is.EqualTo("Rancho grande"));
        }

        [Test]
        public void CanGetCustomerByProperties() {
            IDictionary<string, object> propertyValues = new Dictionary<string, object>();
            propertyValues.Add("CompanyName", "Rancho grande");
            Customer customer = customerRepository.FindOne(propertyValues);

            Assert.That(customer, Is.Not.Null);
            Assert.That(customer.CompanyName, Is.EqualTo("Rancho grande"));

            propertyValues.Add("ContactName", "Won't Match");
            customer = customerRepository.FindOne(propertyValues);

            Assert.That(customer, Is.Null);
        }

        [Test]
        public void CanGetCustomerByNullProperty() {
            IDictionary<string, object> propertyValues = new Dictionary<string, object>();
            propertyValues.Add("Country", null);
            IList<Customer> customers = customerRepository.FindAll(propertyValues);

            Assert.That(customers.Count, Is.EqualTo(1));
            Assert.That(customers[0].ID, Is.EqualTo("ABC12"));
        }

        /// <summary>
        /// This test demonstrates that the orders collection is lazily loaded.  Since lazilly loaded
        /// collections depend on a persisted session, this method is wrapped in a rolled-back transaction 
        /// managed via the superclass.
        /// </summary>
        [Test]
        public void CanLoadCustomerOrders() {
            Customer customer = GetCustomerById();

            Assert.That(customer.Orders.Count, Is.EqualTo(3));
        }

        [Test]
        public void CanFindCustomerOrdersViaCustomFilter() {
            Customer customer = GetCustomerById();

            Assert.That(customer.Orders.FindOrdersPlacedOn(new DateTime(1998, 1, 13)).Count, Is.EqualTo(1));
            Assert.That(customer.Orders.FindOrdersPlacedOn(new DateTime(1992, 10, 13)), Is.Empty);
        }

        /// <summary>
        /// It would be more effecient to use an example object or a more direct means to find the customers
        /// within a particular country, but this is a good demonstration of how the Repository extensions work.
        /// </summary>
        [Test]
        public void CanGetCustomersByCountry() {
            List<Customer> customers = customerRepository.FindByCountry("Brazil");

            Assert.That(customers, Is.Not.Null);
            Assert.That(customers.Count, Is.EqualTo(2));
        }

        [Test]
        [ExpectedException(typeof(PreconditionException))]
        public void CanNotSaveOrUpdatePersistentObjectWithAssignedId() {
            Customer customer = GetCustomerById();
            customerRepository.SaveOrUpdate(customer);
        }

        private Customer GetCustomerById() {
            Customer customer = customerRepository.Load("RANCH", SharpArch.Core.Enums.LockMode.Read);
            Assert.That(customer, Is.Not.Null);
            return customer;
        }

        private Customer CreatePersistedCustomer(string customerId, string contactName, string companyName, string country) {
            Customer customer = new Customer(companyName) {
                ContactName = contactName,
                Country = country
            };
            customer.SetAssignedIdTo(customerId);

            // We have to explicitly call Save() since it has an assigned ID
            customerRepository.Save(customer);
            FlushSessionAndEvict(customer);

            return customer;
        }

        private Order CreatePersistentOrder(Customer customer, DateTime orderDate) {
            Order order = new Order(customer);
            order.OrderDate = orderDate;
            orderRepository.SaveOrUpdate(order);
            FlushSessionAndEvict(order);

            return order;
        }

        private ICustomerRepository customerRepository = new CustomerRepository();
        private IRepository<Order> orderRepository = new Repository<Order>();
    }
}
