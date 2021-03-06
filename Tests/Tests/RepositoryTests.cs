﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using NUnit.Framework;
using Shared.Helper.Test;
using Shared.Repository;
using Tests.Database;
using Tests.Helper;

namespace Tests.Tests
{
    [TestFixture]
    public class RepositoryTests
    {

        //Given I have an Item
        //When I save it and commit it
        //Then It should have an ID
        [Test, Rollback]
        public void AddItemTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");
            var repository = new AccountRepository(databseFactory);

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                //When
                var testAccount = AccountEntityHelper.CreateTestAccount();
                var addedAccount = repository.Save(testAccount);
                unitOfWork.Commit();

                //Then
                Assert.AreNotEqual(addedAccount.AccountID, 0, "The account ID was not updated.");
                EqualityHelper.PropertyValuesAreEqual(addedAccount, testAccount, new[] {"AccountID"});
            }
        }

        //Given I have an item that is missing some information
        //When I save it
        //Then I should receive an exception
        [Test]
        public void AddItemFailsValidationTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");
            var repository = new AccountRepository(databseFactory);
            var testAccount = AccountEntityHelper.CreateTestAccount();
            testAccount.CompanyName = null;
            repository.Save(testAccount);

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                //When
                // ReSharper disable once AccessToDisposedClosure
                TestDelegate testDelegate = () => unitOfWork.Commit();

                Assert.Throws<DbEntityValidationException>(testDelegate);
            }
        }

        //Given I have multiple accounts
        //When I add them
        //Then I should be able to retrieve all of them
        [Test, Rollback]
        public void AddListOfItemsTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");
            var repository = new AccountRepository(databseFactory);
            var listOfItems = AccountEntityHelper.CreateTestAccounts(3);
            var originalItems = repository.GetAll();

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                //When
                repository.AddRange(listOfItems);

                //Then
                var allNewItems = repository.GetAll();
                var itemsAdded = allNewItems.Except(originalItems).ToList();
                EqualityHelper.AssertListsAreEqual(itemsAdded, listOfItems,
                    new[] {"AccountID", "LastModified", "Contacts"});
            }
        }

        //Given I have multiple accounts
        //When I add them
        //Then I should receive an exception
        [Test]
        public void AddListOfItemsFailsValidationTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");
            var repository = new AccountRepository(databseFactory);
            var listOfItems = AccountEntityHelper.CreateTestAccounts(3);
            listOfItems[1].CompanyName = null;

            //When
            TestDelegate testDelegate = () => repository.AddRange(listOfItems);

            //Then
            Assert.Throws<InvalidOperationException>(testDelegate);
        }

        //Given I have a saved account
        //When I update the company name
        //Then I should be able to retrieve it with its new account name
        [Test, Rollback]
        public void UpdateSuccessItemTest()
        {
            //Given
            var databseFactory = new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");
            Account testAccount;
            var repository = new AccountRepository(databseFactory);

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                testAccount = repository.Save(AccountEntityHelper.CreateTestAccount());
                unitOfWork.Commit();

                //When
                testAccount.CompanyName = "Updated account";
                repository.Save(testAccount);
                unitOfWork.Commit();
            }

            //Then
            var finalDatabaseFactory = new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");
            var finalRepo = new AccountRepository(finalDatabaseFactory);
            var itemToCheck = finalRepo.GetById(testAccount.AccountID);
            EqualityHelper.PropertyValuesAreEqual(itemToCheck, testAccount, new[] { "LastModified", "Contacts" });
        }

        //Given I have a saved account
        //When I update the company name to null
        //Then I should not be able to retrieve it with its new account name
        [Test, Rollback]
        public void UpdateFailItemTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");
            Account testAccount;
            var repository = new AccountRepository(databseFactory);

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                testAccount = repository.Save(AccountEntityHelper.CreateTestAccount());
                unitOfWork.Commit();

                //When
                testAccount.CompanyName = null;
                repository.Save(testAccount);
                // ReSharper disable once AccessToDisposedClosure
                TestDelegate testDelegate = () => unitOfWork.Commit();
                Assert.Throws<DbEntityValidationException>(testDelegate);
            }

            //Then
            //Get fresh database factory
            var finalDatabaseFactory = new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");
            var finalRepo = new AccountRepository(finalDatabaseFactory);
            var itemToCheck = finalRepo.GetById(testAccount.AccountID);
            Assert.AreNotEqual(testAccount.CompanyName, itemToCheck.CompanyName, "The company name was updated when it should not have been");

        }

        //Given I have an item
        //When I delete it
        //Then I should not be able to retrieve it
        [Test]
        public void DeleteItemSuccessTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");
            

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                //Given
                var repository = new AccountRepository(databseFactory);
                var testAccount = repository.Save(AccountEntityHelper.CreateTestAccount());
                unitOfWork.Commit();

                //When
                repository.Delete(testAccount);
                unitOfWork.Commit();

                //Then
                var retrievedAccount = repository.GetById(testAccount.AccountID);
                Assert.IsNull(retrievedAccount,"The account was not deleted.");
            }
        }

        //Given I have an item that is not saved
        //When I delete it
        //Then I should receive an exception
        [Test]
        public void DeleteNonExistingItemTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            var repository = new AccountRepository(databseFactory);

            //When
            var testAccount = AccountEntityHelper.CreateTestAccount();
            TestDelegate testDelegate = () => repository.Delete(testAccount);
            
            //Then
            Assert.Throws<InvalidOperationException>(testDelegate);
        }

        //Given I have an items that are saved
        //When I delete them using a where clause
        //Then I should not be able to retrieve them
        [Test]
        public void DeleteWithWhereClauseSuccessTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                var repository = new AccountRepository(databseFactory);
                var listOfItems = AccountEntityHelper.CreateTestAccounts(3);
                var originalItems = repository.GetAll();
                repository.AddRange(listOfItems);
                var allNewItems = repository.GetAll().ToList();
                var itemsAdded = allNewItems.Except(originalItems).ToList();

                //When
                var idsToDelete = itemsAdded.Select(x => x.AccountID);
                repository.Delete(x => idsToDelete.Contains(x.AccountID));
                unitOfWork.Commit();

                //Then
                Assert.AreEqual(0, repository.GetAll().Except(allNewItems).Count(), "The items have not been deleted.");
            }
        }

        //Given I have an item in the database
        //When I get it by ID
        //Then it should match the item in the database
        [Test, Rollback]
        public void RetrieveItemByIdTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");
            var repository = new AccountRepository(databseFactory);
            
            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                var addedAccount = repository.Save(AccountEntityHelper.CreateTestAccount());
                unitOfWork.Commit();

                //When
                var retrievedItem = repository.GetById(addedAccount.AccountID);

                //Then
                EqualityHelper.PropertyValuesAreEqual(retrievedItem, addedAccount);
            }
        }

        //Given I have an ID of an Item that does not exist
        //When I get it by ID
        //Then I should receive an exception
        [Test]
        public void GetByIdExceptionTest()
        {
            //Given
            var databseFactory =
               new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");
            var repository = new AccountRepository(databseFactory);

            //When
            var shouldBeNull =  repository.GetById(-1);

            //Then
            Assert.IsNull(shouldBeNull,"The item was found when it should have never existed.");
        }

        //Given I have items in a db
        //When I get it by a where clause
        //Then I should receive the first item that matches it
        [Test, Rollback]
        public void GetWithWhereClauseSuccessTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                var repository = new AccountRepository(databseFactory);
                var listOfItems = AccountEntityHelper.CreateTestAccounts(3);
                listOfItems[2].CompanyName = "TestReferenceOtherValue";
                repository.AddRange(listOfItems);
                unitOfWork.Commit();

                //When
                var items = repository.Get(x => x.CompanyName.Contains("TestReference"));

                //Then
                EqualityHelper.PropertyValuesAreEqual(items, listOfItems[2],
                    new[] {"AccountID", "LastModified", "LastModifiedBy", "Contacts"});
            }
        }

        //Given I have items no items in a db
        //When I get it by a where clause
        //Then I should receive a null
        [Test]
        public void GetWithWhereClauseButNoneMatchSuccessTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            var repository = new AccountRepository(databseFactory);

            //When
            var shouldBeNull = repository.Get(x => false);

            //Then
            Assert.IsNull(shouldBeNull, "No item should have been found.");
        }

        //Given I items in a DB that matches a clause
        //When I check if any are in the database with the clause
        //Then I should receive true
        [Test, Rollback]
        public void GetAnySuccessTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                var repository = new AccountRepository(databseFactory);
                var listOfItems = AccountEntityHelper.CreateTestAccounts(3);
                listOfItems[2].CompanyName = "TestReferenceOtherValue";
                repository.AddRange(listOfItems);
                unitOfWork.Commit();

                //When
                var hasAny = repository.Any(x => x.CompanyName.Contains("TestReference"));

                //Then
                Assert.IsTrue(hasAny,"None were found when there should have been some.");
            }
        }

        //Given I items in a DB but none matches a clause
        //When I check if any are in the database with the clause
        //Then I should receive false
        [Test]
        public void GetAnyButNoneExistTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            var repository = new AccountRepository(databseFactory);

            //When
            var hasAny = repository.Any(x => false);

            //Then
            Assert.IsFalse(hasAny,"No items should have been found.");
        }

        //Given I items in a DB that matches a clause
        //When I check if none are in the database with the clause
        //Then I should receive false
        [Test, Rollback]
        public void GetNoneButSomeExistTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                var repository = new AccountRepository(databseFactory);
                var listOfItems = AccountEntityHelper.CreateTestAccounts(3);
                listOfItems[2].CompanyName = "TestReferenceOtherValue";
                repository.AddRange(listOfItems);
                unitOfWork.Commit();

                //When
                var hasNone = repository.None(x => x.CompanyName.Contains("TestReference"));

                //Then
                Assert.IsFalse(hasNone, "None were found when there should have been some.");
            }
        }

        //Given I items in a DB but none matches a clause
        //When I check if none are in the database with the clause
        //Then I should receive true
        [Test]
        public void GetNoneSuccessTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            var repository = new AccountRepository(databseFactory);

            //When
            var hasNone = repository.None(x => false);

            //Then
            Assert.IsTrue(hasNone, "No items should have been found.");
        }
        
        //Given I have some items in the database
        //When I get them all
        //Then they should match the items
        [Test, Rollback]
        public void GetAllSuccessTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                var repository = new AccountRepository(databseFactory);
                var listOfItems = AccountEntityHelper.CreateTestAccounts(3);
                listOfItems[0].CompanyName = "1";
                listOfItems[1].CompanyName = "2";
                listOfItems[2].CompanyName = "3";

                repository.AddRange(listOfItems);
                unitOfWork.Commit();

                //When
                var allItems = repository.GetAll();

                //Then
                EqualityHelper.AssertListsAreEqual(allItems.OrderBy(x => x.CompanyName).ToList(),listOfItems.OrderBy(x => x.CompanyName).ToList(),
                    new[] { "AccountID", "LastModified", "LastModifiedBy", "Contacts" });
            }
        }

        //Given I have some items in the database
        //When I get them all
        //Then they should match the items
        [Test]
        public void GetAllButNoneExistTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");
            var repository = new AccountRepository(databseFactory);

            //When
            var allItems = repository.GetAll();

            //Then
            Assert.AreEqual(0,allItems.Count(),"Some items were returned where none should have been");
        }

        //Given I have items in a db
        //When I get it by a where clause
        //Then I should receive all the items that matches it
        [Test, Rollback]
        public void GetManyWithWhereClauseSuccessTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                var repository = new AccountRepository(databseFactory);
                var reference = "TestReference";
                var listOfItems = GetItemsWithTwoItemsContainingTestReference(reference);
                repository.AddRange(listOfItems);
                unitOfWork.Commit();

                //When
                var items = repository.GetMany(x => x.CompanyName.Contains(reference));

                //Then
                EqualityHelper.AssertListsAreEqual(items.OrderBy(x => x.CompanyName).ToList(), listOfItems.OrderBy(x => x.CompanyName).Take(2).ToList(),new[] { "AccountID", "LastModified", "LastModifiedBy", "Contacts" });
            }
        }

        //Given I have items no items in a db
        //When I get it by a where clause
        //Then I should receive an empty list
        [Test]
        public void GetManyWithWhereClauseButNoneMatchSuccessTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            var repository = new AccountRepository(databseFactory);

            //When
            var shouldBeEmpty = repository.GetMany(x => false);

            //Then
            Assert.IsFalse(shouldBeEmpty.Any(), "No item should have been found.");
        }

        //Given I have items in a db
        //When I get it by a where clause for an autocomplete
        //Then I should receive the first item that matches it
        [Test,Rollback]
        public void GetAutoCompleteItemsTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                var repository = new AccountRepository(databseFactory);
                var reference = "TestReference";
                var listOfItems = GetItemsWithTwoItemsContainingTestReference(reference);
                repository.AddRange(listOfItems);
                unitOfWork.Commit();

                //When
                var item = repository.GetAutoCompleteItems(x => x.CompanyName.Contains(reference),1);

                //Then
                EqualityHelper.PropertyValuesAreEqual(item.First(),listOfItems[0], new[] { "AccountID", "LastModified", "LastModifiedBy", "Contacts" });
            }
        }

        //Given I have items no items in a db
        //When I get it by a where clause
        //Then I should receive an empty list
        [Test]
        public void GetAutocompleteItemsButNoneExist()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            var repository = new AccountRepository(databseFactory);

            //When
            var shouldBeEmpty = repository.GetAutoCompleteItems(x => false, 1);

            //Then
            Assert.IsFalse(shouldBeEmpty.Any(), "No item should have been found.");
        }

        //Given I have a stored procedure and accounts in the database
        //When I get all the items via the stored procedure with a filter
        //Then it should match the all the accounts in the database that match the filter
        [Test, Rollback]
        public void GetItemsViaStoredProcedureWithParameterTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                var repository = new AccountRepository(databseFactory);
                var reference = "TestReference";
                var listOfItems = GetItemsWithTwoItemsContainingTestReference(reference);
                repository.AddRange(listOfItems);
                unitOfWork.Commit();

                //When
                var filter = new SqlParameter("@CompanyName", SqlDbType.VarChar) {Value = $"%{reference}%"};
                var items = repository.ExecuteQuery("exec GetAccounts @CompanyName", filter).ToList();

                //Then
                EqualityHelper.AssertListsAreEqual(items.OrderBy(x => x.CompanyName).ToList(), listOfItems.OrderBy(x => x.CompanyName).Take(2).ToList(), new[] { "AccountID", "LastModified", "LastModifiedBy", "Contacts" });
            }
        }

        //Given I have a stored procedure and accounts in the database
        //When I get all the items via the stored procedure
        //Then it should match the all the accounts in the database
        [Test, Rollback]
        public void GetItemsViaStoredProcedureWithNoParameterTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                var repository = new AccountRepository(databseFactory);
                var listOfItems = AccountEntityHelper.CreateTestAccounts(3);
                repository.AddRange(listOfItems);
                unitOfWork.Commit();

                //When
                var items = repository.ExecuteQuery("exec GetAccounts").ToList();

                //Then
                EqualityHelper.AssertListsAreEqual(items.OrderBy(x => x.CompanyName).ToList(), listOfItems.OrderBy(x => x.CompanyName).ToList(), new[] { "AccountID", "LastModified", "LastModifiedBy", "Contacts" });
            }
        }

        //Given I have a stored procedure and no accounts in the database
        //When I get all the items via the stored procedure
        //Then it should return an empty list
        [Test]
        public void GetItemsViaStoredProcedureWithNoParameterButNoAccountsExistTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            //When
            var repository = new AccountRepository(databseFactory);
            var items = repository.ExecuteQuery("exec GetAccounts").ToList();

            //Then
            Assert.AreEqual(0, items.Count, "Some items were returned where none should have been");
        }

        //Given I have a broken query
        //When I execute the query
        //Then I should receive an exception
        [Test, Rollback]
        public void ExecuteStoredProcedureWithIncorrectQueryTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            //When
            var repository = new AccountRepository(databseFactory);
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            TestDelegate testDelegate = () => repository.ExecuteQuery("exec GetAccountsFake").ToList();

            //Then
            Assert.Throws<SqlException>(testDelegate, "The expected exception was not thrown.");
        }

        //Given I have a stored procedure and accounts in the database
        //When I get all the items via the stored procedure with a filter
        //Then it should match the all the accounts in the database that match the filter
        [Test, Rollback]
        public void GetItemsViaStoredProcedureWithParameterNotParameterisedTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                var repository = new AccountRepository(databseFactory);
                var reference = "TestReference";
                var listOfItems = GetItemsWithTwoItemsContainingTestReference(reference);
                repository.AddRange(listOfItems);
                unitOfWork.Commit();

                //When
                var filter = new SqlParameter("@CompanyName", SqlDbType.VarChar) { Value = $"%{reference}%" };
                var items = repository.ExecuteQuery<Account>("exec GetAccounts @CompanyName", filter).ToList();

                //Then
                EqualityHelper.AssertListsAreEqual(items.OrderBy(x => x.CompanyName).ToList(), listOfItems.OrderBy(x => x.CompanyName).Take(2).ToList(), new[] { "AccountID", "LastModified", "LastModifiedBy", "Contacts" });
            }
        }

        //Given I have a stored procedure and accounts in the database
        //When I get all the items via the stored procedure
        //Then it should match the all the accounts in the database
        [Test, Rollback]
        public void GetItemsViaStoredProcedureWithNoParameterNotParameterisedTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                var repository = new AccountRepository(databseFactory);
                var listOfItems = AccountEntityHelper.CreateTestAccounts(3);
                repository.AddRange(listOfItems);
                unitOfWork.Commit();

                //When
                var items = repository.ExecuteQuery<Account>("exec GetAccounts").ToList();

                //Then
                EqualityHelper.AssertListsAreEqual(items.OrderBy(x => x.CompanyName).ToList(), listOfItems.OrderBy(x => x.CompanyName).ToList(), new[] { "AccountID", "LastModified", "LastModifiedBy", "Contacts" });
            }
        }

        //Given I have a stored procedure and no accounts in the database
        //When I get all the items via the stored procedure
        //Then it should return an empty list
        [Test]
        public void GetItemsViaStoredProcedureWithNoParameterButNoAccountsExistNotParameterisedTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            //When
            var repository = new AccountRepository(databseFactory);
            var items = repository.ExecuteQuery<Account>("exec GetAccounts").ToList();

            //Then
            Assert.AreEqual(0, items.Count, "Some items were returned where none should have been");
        }

        //Given I have a stored procedure and accounts in the database
        //When I clear all the items in the database via the stored procedure
        //Then I should not be able to retrieve the item
        [Test, Rollback]
        public void ExecuteStoredProcedureAsActionTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");

            using (var unitOfWork = new UnitOfWork<SharedCommonDatabaseContext>(databseFactory))
            {
                var repository = new AccountRepository(databseFactory);
                var listOfItems = AccountEntityHelper.CreateTestAccounts(3);
                repository.AddRange(listOfItems);
                unitOfWork.Commit();

                //When
                repository.ExecuteQueryAsAction("exec DeleteAllAccounts");

                //Then
                Assert.AreEqual(0,repository.GetAll().Count(), "There are still items in the database.");
            }
        }

        //Given I have a broken query
        //When Execute the stored procedure
        //Then I should receive an exception
        [Test]
        public void ExecuteStoredProcedureAsActionWithBrokenSyntaxTest()
        {
            //Given
            var databseFactory =
                new DatabaseFactoryBase<SharedCommonDatabaseContext>("SharedCommonDatabaseContext");
            var repository = new AccountRepository(databseFactory);

            //When
            TestDelegate testDelegate = () => repository.ExecuteQueryAsAction("exec DeleteAllAccountsFake");

            //Then
            Assert.Throws<SqlException>(testDelegate);
        }

       

        private static List<Account> GetItemsWithTwoItemsContainingTestReference(string reference)
        {
            var listOfItems = AccountEntityHelper.CreateTestAccounts(3);
            listOfItems[0].CompanyName = $"1{reference}";
            listOfItems[1].CompanyName = $"2{reference}OtherValue";
            listOfItems[2].CompanyName = "3";
            return listOfItems;
        }
    }
}
