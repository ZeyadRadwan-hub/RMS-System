using System.ComponentModel.DataAnnotations;
using RMS_BACKEND.DTOs;
using Xunit;

namespace RMS_BACKEND.Tests;

public sealed class DtoValidationTests
{
    public static IEnumerable<object[]> InvalidInputs()
    {
        yield return new object[] { new LoginRequestDto { Code = new string('x', 51), Password = "valid-secret" } };
        yield return new object[] { new LoginRequestDto { Code = "code", Password = new string('x', 129) } };
        yield return new object[] { new ChangePasswordRequestDto { CurrentPassword = "old-secret", NewPassword = "short" } };
        yield return new object[] { new ChangePasswordRequestDto { CurrentPassword = "", NewPassword = new string('x', 12) } };
        yield return new object[] { new UpdateEmployeeDto { Code = "code", Name = "name", EmployeeRole = 2, EmployeeLevelId = 1, DepartmentID = 7, DateOfEmployment = new DateTime(2020, 1, 1) } };
        yield return new object[] { new UpdateEmployeeDto { Code = "code", Name = "name", EmployeeLevelId = 1, DepartmentID = 7 } };
        yield return new object[] { new EmployeeFilterDto { Code = new string('x', 51) } };
        yield return new object[] { new EmployeeFilterDto { Name = new string('x', 201) } };
        yield return new object[] { new EmployeeFilterDto { DepartmentID = -1 } };
        yield return new object[] { new EmployeeFilterDto { EmployeeLevelId = 0 } };
        yield return new object[] { new LeaveBalanceReportRequestDto { EmployeeId = 0 } };
        yield return new object[] { new DashboardFilterDto { StatusID = -1 } };
        yield return new object[] { new DashboardFilterDto { DepartmentID = -1 } };
        yield return new object[] { new DashboardFilterDto { EmployeeId = -1 } };
        yield return new object[] { new DashboardFilterDto { GroupBy = "arbitrary" } };
        yield return new object[] { new UpdateTransactionFormDto { TransactionTypesID = 1, StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 1), LeaveRationale = new string('x', 501) } };
        yield return new object[] { new CreateTransactionFormDto { TransactionTypesID = 1, StartDate = new DateTime(1999, 1, 1), EndDate = new DateTime(1999, 1, 1), LeaveRationale = "reason" } };
        yield return new object[] { new CreateTransactionRequestDto { TransactionTypesID = 1, StartDate = new DateTime(2101, 1, 1), EndDate = new DateTime(2101, 1, 1), LeaveRationale = "reason" } };
    }

    [Trait("Category", "Validation")]
    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public void Request_dtos_reject_out_of_contract_inputs(object request) => Assert.NotEmpty(Validate(request));

    [Trait("Category", "Validation")]
    [Fact]
    public void Valid_boundary_inputs_and_route_owned_ids_are_accepted()
    {
        Assert.Empty(Validate(new CreateEmployeeDto
        {
            Code = new string('c', 50), Name = new string('n', 200), Password = new string('p', 9),
            DepartmentID = 7, EmployeeLevelId = 1, DateOfEmployment = new DateTime(2020, 1, 1)
        }));
        Assert.Empty(Validate(new UpdateEmployeeDto
        {
            Code = "code", Name = "name", DepartmentID = 7, EmployeeLevelId = 1,
            DateOfEmployment = new DateTime(2020, 1, 1)
        }));
        Assert.Empty(Validate(new UpdateTransactionRequestDto
        {
            TransactionTypesID = 2, StartDate = new DateTime(2026, 10, 10),
            EndDate = new DateTime(2026, 10, 10), LeaveRationale = new string('r', 500)
        }));
        Assert.Empty(Validate(new EmployeeFilterDto()));
        Assert.Empty(Validate(new ChangePasswordRequestDto
        {
            CurrentPassword = "old-secret", NewPassword = new string('p', 9)
        }));
        Assert.Empty(Validate(new DashboardFilterDto()));
        Assert.Empty(Validate(new DashboardFilterDto { GroupBy = "Department" }));
        Assert.Empty(Validate(new ApproveRejectRequestDto { ResponseMessage = new string('r', 1000) }));
    }

    [Trait("Category", "Validation")]
    [Fact]
    public void Create_employee_rejects_missing_fields_invalid_role_and_invalid_ids()
    {
        var request = new CreateEmployeeDto
        {
            Code = "",
            Name = "",
            Password = "short",
            DateOfEmployment = default,
            EmployeeRole = 9,
            EmployeeLevelId = 0,
            DepartmentID = 0
        };

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateEmployeeDto.Code)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateEmployeeDto.Name)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateEmployeeDto.Password)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateEmployeeDto.DateOfEmployment)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateEmployeeDto.EmployeeRole)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateEmployeeDto.EmployeeLevelId)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateEmployeeDto.DepartmentID)));
    }

    [Trait("Category", "Validation")]
    [Fact]
    public void Transaction_request_rejects_default_dates_reversed_dates_and_missing_reason()
    {
        var request = new CreateTransactionRequestDto
        {
            TransactionTypesID = 0,
            StartDate = default,
            EndDate = default,
            LeaveRationale = ""
        };

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateTransactionRequestDto.TransactionTypesID)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateTransactionRequestDto.StartDate)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateTransactionRequestDto.EndDate)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(CreateTransactionRequestDto.LeaveRationale)));

        request.StartDate = new DateTime(2026, 10, 10);
        request.EndDate = new DateTime(2026, 10, 9);
        request.TransactionTypesID = 1;
        request.LeaveRationale = "Valid reason";
        errors = Validate(request);

        Assert.Contains(errors, error => error.ErrorMessage!.Contains("on or after", StringComparison.OrdinalIgnoreCase));
    }

    [Trait("Category", "Validation")]
    [Fact]
    public void Approval_accepts_route_owned_id_and_rejects_oversized_messages()
    {
        var approvalErrors = Validate(new ApproveRejectRequestDto
        {
            TransactionId = 0,
            ResponseMessage = new string('x', 1001)
        });
        var cancelErrors = Validate(new CancelRequestDto { TransactionId = 0 });

        Assert.Empty(Validate(new ApproveRejectRequestDto { ResponseMessage = "Approved" }));
        Assert.Contains(approvalErrors, error => error.MemberNames.Contains(nameof(ApproveRejectRequestDto.ResponseMessage)));
        Assert.Contains(cancelErrors, error => error.MemberNames.Contains(nameof(CancelRequestDto.TransactionId)));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        if (model is IValidatableObject validatable)
            results.AddRange(validatable.Validate(context));
        return results;
    }
}
