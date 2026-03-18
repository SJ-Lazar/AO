using AO.Core.Features.Companies;
using AO.Core.Features.Contacts;
using AO.Core.Features.Users;
using AO.Tests.Features;

namespace AO.Tests.Features.Contacts;

[TestFixture]
public sealed class ContactSliceTests
{
    [Test]
    public void CreateAsync_WithUnknownCompany_ThrowsArgumentException()
    {
        Assert.That(async () =>
        {
            await using var scope = await FeatureTestDbScope.CreateAsync();
            await ContactSlice.CreateAsync(
                scope.DbContext,
                new CreateContactRequest
                {
                    FirstName = "Mikasa",
                    LastName = "Ackerman",
                    CompanyId = Guid.NewGuid()
                },
                CancellationToken.None);
        }, Throws.InstanceOf<ArgumentException>().With.Message.EqualTo("The selected company was not found."));
    }

    [Test]
    public async Task UpdateAsync_WithValidRequest_UpdatesContactAndProjectsCompanyName()
    {
        await using var scope = await FeatureTestDbScope.CreateAsync();
        var company = new Company { Name = "Scout Regiment" };
        var user = new CrmUser { FirstName = "Levi", LastName = "Ackerman", Email = "levi@ao.example" };
        var contact = new Contact { FirstName = "Eren", LastName = "Yeager" };

        scope.DbContext.AddRange(company, user);
        scope.DbContext.Contacts.Add(contact);
        await scope.DbContext.SaveChangesAsync();

        var result = await ContactSlice.UpdateAsync(
            scope.DbContext,
            contact.Id,
            new UpdateContactRequest
            {
                FirstName = " Armin ",
                LastName = " Arlert ",
                Email = " armin@scout.example ",
                Phone = " 555-0123 ",
                JobTitle = " Strategist ",
                Notes = " Key advisor ",
                CompanyId = company.Id,
                AssignedUserId = user.Id,
                IsArchived = true
            },
            CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.FirstName, Is.EqualTo("Armin"));
            Assert.That(result.LastName, Is.EqualTo("Arlert"));
            Assert.That(result.FullName, Is.EqualTo("Armin Arlert"));
            Assert.That(result.CompanyId, Is.EqualTo(company.Id));
            Assert.That(result.CompanyName, Is.EqualTo("Scout Regiment"));
            Assert.That(result.AssignedUserId, Is.EqualTo(user.Id));
            Assert.That(result.AssignedUserName, Is.EqualTo("Levi Ackerman"));
            Assert.That(result.IsArchived, Is.True);
        });
    }
}
