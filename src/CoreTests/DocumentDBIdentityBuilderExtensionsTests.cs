﻿namespace CoreTests
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.DocumentDB;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class DocumentDBIdentityBuilderExtensionsTests : AssertionHelper
    {
        static readonly string databaseId = "AspDotNetCore.Identity.DocumentDB.Test";
        static readonly string key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        DocumentClient client = new DocumentClient(new Uri("https://localhost:8081"), key);

        [Test]
        public void AddDocumentDBStores_WithDefaultTypes_ResolvesStoresAndManagers()
        {
            var services = new ServiceCollection();
            services.AddIdentityWithDocumentDBStores(client, UriFactory.CreateDatabaseUri(databaseId).ToString());
            // note: UserManager and RoleManager use logging
            services.AddLogging();

            var provider = services.BuildServiceProvider();
            var resolvedUserStore = provider.GetService<IUserStore<IdentityUser>>();
            Expect(resolvedUserStore, Is.Not.Null, "User store did not resolve");

            var resolvedRoleStore = provider.GetService<IRoleStore<IdentityRole>>();
            Expect(resolvedRoleStore, Is.Not.Null, "Role store did not resolve");

            var resolvedUserManager = provider.GetService<UserManager<IdentityUser>>();
            Expect(resolvedUserManager, Is.Not.Null, "User manager did not resolve");

            var resolvedRoleManager = provider.GetService<RoleManager<IdentityRole>>();
            Expect(resolvedRoleManager, Is.Not.Null, "Role manager did not resolve");
        }

        protected class CustomUser : IdentityUser
        {
        }

        protected class CustomRole : IdentityRole
        {
        }

        [Test]
        public void AddDocumentDBStores_WithCustomTypes_ThisShouldLookReasonableForUsers()
        {
            // this test is just to make sure I consider the interface for using custom types
            // so that it's not a horrible experience even though it should be rarely used
            var services = new ServiceCollection();
            services.AddIdentityWithDocumentDBStoresUsingCustomTypes<CustomUser, CustomRole>(client, UriFactory.CreateDatabaseUri(databaseId).ToString());
            services.AddLogging();

            var provider = services.BuildServiceProvider();
            var resolvedUserStore = provider.GetService<IUserStore<CustomUser>>();
            Expect(resolvedUserStore, Is.Not.Null, "User store did not resolve");

            var resolvedRoleStore = provider.GetService<IRoleStore<CustomRole>>();
            Expect(resolvedRoleStore, Is.Not.Null, "Role store did not resolve");

            var resolvedUserManager = provider.GetService<UserManager<CustomUser>>();
            Expect(resolvedUserManager, Is.Not.Null, "User manager did not resolve");

            var resolvedRoleManager = provider.GetService<RoleManager<CustomRole>>();
            Expect(resolvedRoleManager, Is.Not.Null, "Role manager did not resolve");
        }

        [Test]
        public void AddDocumentDBStores_ConnectionStringWithoutDatabase_Throws()
        {
            string databaseId = "fake";
            TestDelegate addDocumentDbStores = () => new ServiceCollection()
                .AddIdentity<IdentityUser, IdentityRole>()
                .RegisterDocumentDBStores<IdentityUser, IdentityRole>(client, UriFactory.CreateDatabaseUri(databaseId).ToString());

            Expect(addDocumentDbStores, Throws.Exception
                .With.InnerException.InnerException.Message.Contains("The server name or address could not be resolved"));
        }

        protected class WrongUser : IdentityUser
        {
        }

        protected class WrongRole : IdentityRole
        {
        }

        [Test]
        public void AddDocumentDBStores_MismatchedTypes_ThrowsWarningToHelpUsers()
        {
            Expect(() => new ServiceCollection()
                    .AddIdentity<IdentityUser, IdentityRole>()
                    .RegisterDocumentDBStores<WrongUser, IdentityRole>(client, UriFactory.CreateDatabaseUri(databaseId).ToString()),
                Throws.Exception.With.Message
                    .EqualTo("User type passed to RegisterDocumentDBStores must match user type passed to AddIdentity. You passed Microsoft.AspNetCore.Identity.DocumentDB.IdentityUser to AddIdentity and CoreTests.DocumentDBIdentityBuilderExtensionsTests+WrongUser to RegisterDocumentDBStores, these do not match.")
            );

            Expect(() => new ServiceCollection()
                    .AddIdentity<IdentityUser, IdentityRole>()
                    .RegisterDocumentDBStores<IdentityUser, WrongRole>(client, UriFactory.CreateDatabaseUri(databaseId).ToString()),
                Throws.Exception.With.Message
                    .EqualTo("Role type passed to RegisterDocumentDBStores must match role type passed to AddIdentity. You passed Microsoft.AspNetCore.Identity.DocumentDB.IdentityRole to AddIdentity and CoreTests.DocumentDBIdentityBuilderExtensionsTests+WrongRole to RegisterDocumentDBStores, these do not match.")
            );
        }
    }
}