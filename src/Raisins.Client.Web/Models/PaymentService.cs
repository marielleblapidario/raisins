﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Raisins.Services;
using Castle.ActiveRecord;

namespace Raisins.Client.Web.Models
{
    public class PaymentService
    {

        #region Operations

        public static PaymentModel[] FindAll()
        {
            var account = Account.FindUser(HttpContext.Current.User.Identity.Name);

            Payment[] payments = null;
            if (account.Role.RoleType == RoleType.User)
            {
                payments = Payment.FindByUser(HttpContext.Current.User.Identity.Name);
            }
            else
            {
                payments = Payment.FindByBeneficiary(account.Settings.First().Beneficiary.Name);
            }
            
            List<PaymentModel> models = new List<PaymentModel>();

            foreach (Payment payment in payments)
            {
                models.Add(ToModel(payment));
            }

            return models.ToArray();
        }

        public static void Save(PaymentModel model)
        {
            Payment data = ToData(model);
            
            //assign settings specific items
            Account account = Account.FindUser(HttpContext.Current.User.Identity.Name);
            Setting setting = account.Settings.FirstOrDefault();
            if (setting != null)
            {
                data.Location = setting.Location;
                data.Currency = setting.Currency;
                data.Class = setting.Class;
                data.CreatedBy = account;
            }
            
            data.Save();
        }

        public static void Update(PaymentModel modelToBeUpdated)
        {
            Save(modelToBeUpdated);
        }

        public static void Delete(long id)
        {
            IList<long> idsToDelete = new List<long>();
            idsToDelete.Add(id);
            Payment.DeleteAll(idsToDelete);
        }

        public static PaymentModel GetPayment(long id)
        {
            return ToModel(Payment.Find(id));
        }

        public static PaymentModel[] FindAllByUser(string userName)
        {
            List<PaymentModel> results = new List<PaymentModel>();
            var payments = Payment.FindByUser(userName);

            foreach (var payment in payments)
            {
                results.Add(ToModel(payment));
            }

            return results.ToArray();
        }

        public static void LockAll()
        {
            TransactionScope transaction = new TransactionScope();

            try
            {
                var account = Account.FindUser(HttpContext.Current.User.Identity.Name);
                var payments = Payment.FindByBeneficiary(account.Settings.First().Beneficiary.Name);

                foreach (var payment in payments)
                {
                    if (!payment.Locked)
                    {
                        payment.Locked = true;
                        payment.AuditedBy = account;

                        generateTicket(payment);

                        payment.Update();
                    }
                }
            }
            catch
            {
                transaction.VoteRollBack();
                throw;
            }
            finally
            {
                transaction.Dispose();
            }
        }

        private static void generateTicket(Payment payment)
        {
            int ticketCount = Convert.ToInt32(payment.Amount / payment.Currency.Ratio);

            List<Ticket> tickets = new List<Ticket>();

            for (int i = 0; i < ticketCount; i++)
            {
                Ticket ticket = new Ticket() { Name = payment.Name };
                ticket.Payment = payment;
                ticket.Save();
            }
        }

        public static PaymentModel CreateNew()
        {
            PaymentModel model = new PaymentModel();
            
            //load settings
            Setting setting = Account.FindUser(HttpContext.Current.User.Identity.Name).Settings.FirstOrDefault();

            if (setting != null)
            {
                model.Currency = setting.Currency.CurrencyCode;
                model.Class = setting.Class;
            }

            return model;
        }

        #endregion

        #region Helper methods

        protected static PaymentModel ToModel(Payment data)
        {
            PaymentModel model = new PaymentModel();

            model.ID = data.ID;
            model.Amount = data.Amount;
            model.Class = data.Class;
            model.Email = data.Email;
            model.Name = data.Name;
            model.Currency = data.Currency.CurrencyCode;
            model.Beneficiary = data.Beneficiary.Name;
            model.Locked = data.Locked;

            return model;
        }

        protected static Payment ToData(PaymentModel model)
        {
            Payment data = new Payment();
            data.Amount = model.Amount;
            data.Email = model.Email;
            data.ID = model.ID;
            data.Name = model.Name;
            data.Beneficiary = Beneficiary.FindByName(model.Beneficiary);

            return data;
        }

        #endregion






        
    }
}