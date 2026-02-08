namespace Hrevolve.Web.Controllers;

/// <summary>
/// 保险福利控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InsuranceController : ControllerBase
{
    private static readonly object BenefitsLock = new();
    private static List<BenefitSimpleDto> BenefitsCache = SeedBenefits();

    /// <summary>
    /// 获取保险统计
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetInsuranceStats()
    {
        return Ok(new { totalPlans = 5, enrolledEmployees = 120, monthlyPremium = 156000m, pendingClaims = 3 });
    }

    /// <summary>
    /// 获取保险方案列表
    /// </summary>
    [HttpGet("plans")]
    public IActionResult GetInsurancePlans()
    {
        return Ok(new[]
        {
            new { id = Guid.NewGuid(), name = "基本医疗保险", type = "health", premium = 500m, coverage = 500000m, isActive = true, description = "基本医疗保障" },
            new { id = Guid.NewGuid(), name = "补充医疗保险", type = "health", premium = 200m, coverage = 200000m, isActive = true, description = "补充医疗保障" },
            new { id = Guid.NewGuid(), name = "意外伤害保险", type = "accident", premium = 100m, coverage = 1000000m, isActive = true, description = "意外伤害保障" }
        });
    }

    /// <summary>
    /// 创建保险方案
    /// </summary>
    [HttpPost("plans")]
    public IActionResult CreateInsurancePlan([FromBody] object data)
    {
        return Ok(new { id = Guid.NewGuid(), message = "创建成功" });
    }

    /// <summary>
    /// 更新保险方案
    /// </summary>
    [HttpPut("plans/{id:guid}")]
    public IActionResult UpdateInsurancePlan(Guid id, [FromBody] object data)
    {
        return Ok(new { message = "更新成功" });
    }

    /// <summary>
    /// 删除保险方案
    /// </summary>
    [HttpDelete("plans/{id:guid}")]
    public IActionResult DeleteInsurancePlan(Guid id)
    {
        return Ok(new { message = "删除成功" });
    }

    /// <summary>
    /// 获取员工参保列表
    /// </summary>
    [HttpGet("employees")]
    public IActionResult GetEmployeeInsurances([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(new
        {
            items = new[]
            {
                new { id = Guid.NewGuid(), employeeId = Guid.NewGuid(), employeeName = "张三", planId = Guid.NewGuid(), planName = "基本医疗保险", startDate = "2024-01-01", premium = 500m, status = "active" },
                new { id = Guid.NewGuid(), employeeId = Guid.NewGuid(), employeeName = "李四", planId = Guid.NewGuid(), planName = "基本医疗保险", startDate = "2024-01-01", premium = 500m, status = "active" }
            },
            total = 2,
            page,
            pageSize
        });
    }

    /// <summary>
    /// 员工参保
    /// </summary>
    [HttpPost("enroll")]
    public IActionResult EnrollEmployeeInsurance([FromBody] object data)
    {
        return Ok(new { id = Guid.NewGuid(), message = "参保成功" });
    }

    /// <summary>
    /// 终止员工保险
    /// </summary>
    [HttpPost("employees/{id:guid}/terminate")]
    public IActionResult TerminateEmployeeInsurance(Guid id)
    {
        return Ok(new { message = "已终止" });
    }

    /// <summary>
    /// 获取福利项目列表
    /// </summary>
    [HttpGet("benefits-simple")]
    public IActionResult GetBenefits()
    {
        lock (BenefitsLock)
        {
            return Ok(BenefitsCache.ToList());
        }
    }

    /// <summary>
    /// 创建福利项目
    /// </summary>
    [HttpPost("benefits-simple")]
    public IActionResult CreateBenefit([FromBody] BenefitSimpleUpsert data)
    {
        if (string.IsNullOrWhiteSpace(data.name) || string.IsNullOrWhiteSpace(data.type))
        {
            return BadRequest(new { message = "name/type 不能为空" });
        }

        var created = new BenefitSimpleDto(
            Guid.NewGuid(),
            data.name.Trim(),
            data.type.Trim(),
            data.amount ?? 0m,
            data.isActive ?? true,
            data.description);

        lock (BenefitsLock)
        {
            BenefitsCache = [created, ..BenefitsCache];
        }

        return Ok(created);
    }

    /// <summary>
    /// 更新福利项目
    /// </summary>
    [HttpPut("benefits-simple/{id:guid}")]
    public IActionResult UpdateBenefit(Guid id, [FromBody] BenefitSimpleUpsert data)
    {
        lock (BenefitsLock)
        {
            var idx = BenefitsCache.FindIndex(x => x.id == id);
            if (idx < 0) return NotFound();

            var old = BenefitsCache[idx];
            var updated = old with
            {
                name = string.IsNullOrWhiteSpace(data.name) ? old.name : data.name.Trim(),
                type = string.IsNullOrWhiteSpace(data.type) ? old.type : data.type.Trim(),
                amount = data.amount ?? old.amount,
                isActive = data.isActive ?? old.isActive,
                description = data.description ?? old.description
            };

            BenefitsCache[idx] = updated;
            return Ok(updated);
        }
    }

    /// <summary>
    /// 删除福利项目
    /// </summary>
    [HttpDelete("benefits-simple/{id:guid}")]
    public IActionResult DeleteBenefit(Guid id)
    {
        lock (BenefitsLock)
        {
            BenefitsCache = BenefitsCache.Where(x => x.id != id).ToList();
        }
        return Ok(new { message = "ok" });
    }

    private static List<BenefitSimpleDto> SeedBenefits() =>
    [
        new(Guid.NewGuid(), "餐饮补贴", "meal", 500m, true, "每月餐饮补贴"),
        new(Guid.NewGuid(), "交通补贴", "transport", 300m, true, "每月交通补贴"),
        new(Guid.NewGuid(), "通讯补贴", "communication", 200m, true, "每月通讯补贴")
    ];

    public record BenefitSimpleDto(Guid id, string name, string type, decimal amount, bool isActive, string? description);
    public record BenefitSimpleUpsert(string? name, string? type, decimal? amount, string? description, bool? isActive);
}
