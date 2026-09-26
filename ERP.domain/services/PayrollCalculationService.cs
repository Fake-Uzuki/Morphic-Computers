using System;

namespace ERP.domain.services
{
    public class PayrollCalculationResult
    {
        public decimal BaseSalary { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal GrossPay { get; set; }

        public decimal SssDeduction { get; set; }
        public decimal PhilHealthDeduction { get; set; }
        public decimal PagIbigDeduction { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal WithholdingTax { get; set; }
        public decimal OtherDeductions { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetPay { get; set; }
    }

    /// <summary>
    /// Philippine Statutory Payroll Deduction Engine.
    /// Implements current statutory rules and circulars:
    /// - SSS: Republic Act No. 11199 / SSS Circular No. 2024-006 (Effective Jan 1, 2025 onwards: 15% total, 5% employee share, MSC 5k-35k).
    /// - PhilHealth: Republic Act No. 11223 (Universal Health Care Act, 5% premium rate, 2.5% employee share, floor 10k, ceiling 100k).
    /// - Pag-IBIG (HDMF): Circular No. 460 (Effective Feb 1, 2024 onwards: 2% employee share capped at max fund salary 10k -> 200.00/mo).
    /// - BIR Withholding Tax: Republic Act No. 10963 (TRAIN Law), Revenue Regulations No. 11-2018 (Revised Withholding Tax Table effective Jan 1, 2023 onwards).
    /// </summary>
    public static class PayrollCalculationService
    {
        /// <summary>
        /// SSS Employee Share: 5.0% of Monthly Salary Credit (MSC).
        /// MSC minimum is 5,000 (below 5,250); MSC maximum is 35,000 (34,750 and above) in steps of 500.
        /// </summary>
        public static decimal CalculateSssContribution(decimal monthlyCompensation)
        {
            if (monthlyCompensation <= 0) return 0m;
            if (monthlyCompensation < 5250m) return 250.00m; // MSC 5,000 * 5%
            if (monthlyCompensation >= 34750m) return 1750.00m; // MSC 35,000 * 5%

            decimal msc = Math.Floor((monthlyCompensation - 250m) / 500m) * 500m + 500m;
            return Math.Round(msc * 0.05m, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// PhilHealth Employee Share: 2.5% (half of 5.0% premium).
        /// Income floor: 10,000 (min 250.00); Income ceiling: 100,000 (max 2,500.00).
        /// </summary>
        public static decimal CalculatePhilHealthContribution(decimal monthlyCompensation)
        {
            if (monthlyCompensation <= 0) return 0m;
            decimal cappedSalary = Math.Clamp(monthlyCompensation, 10000m, 100000m);
            return Math.Round(cappedSalary * 0.025m, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Pag-IBIG Employee Share: 1% if <= 1,500; 2% if > 1,500.
        /// Maximum monthly fund salary is 10,000 -> maximum employee deduction is 200.00.
        /// </summary>
        public static decimal CalculatePagIbigContribution(decimal monthlyCompensation)
        {
            if (monthlyCompensation <= 0) return 0m;
            if (monthlyCompensation <= 1500m)
            {
                return Math.Round(monthlyCompensation * 0.01m, 2, MidpointRounding.AwayFromZero);
            }
            return Math.Min(200.00m, Math.Round(monthlyCompensation * 0.02m, 2, MidpointRounding.AwayFromZero));
        }

        /// <summary>
        /// BIR Withholding Tax on monthly taxable compensation under TRAIN Law (RA 10963, RR 11-2018).
        /// </summary>
        public static decimal CalculateWithholdingTax(decimal taxableIncome)
        {
            if (taxableIncome <= 20833.00m)
            {
                return 0m;
            }
            else if (taxableIncome <= 33333.00m)
            {
                return Math.Round((taxableIncome - 20833.00m) * 0.15m, 2, MidpointRounding.AwayFromZero);
            }
            else if (taxableIncome <= 66667.00m)
            {
                return Math.Round(1875.00m + ((taxableIncome - 33333.00m) * 0.20m), 2, MidpointRounding.AwayFromZero);
            }
            else if (taxableIncome <= 166667.00m)
            {
                return Math.Round(8541.80m + ((taxableIncome - 66667.00m) * 0.25m), 2, MidpointRounding.AwayFromZero);
            }
            else if (taxableIncome <= 666667.00m)
            {
                return Math.Round(33541.80m + ((taxableIncome - 166667.00m) * 0.30m), 2, MidpointRounding.AwayFromZero);
            }
            else
            {
                return Math.Round(183541.80m + ((taxableIncome - 666667.00m) * 0.35m), 2, MidpointRounding.AwayFromZero);
            }
        }

        /// <summary>
        /// Calculates complete Philippine statutory and voluntary deductions, gross pay, and net take-home pay.
        /// </summary>
        public static PayrollCalculationResult Calculate(
            decimal baseSalary,
            decimal overtimePay = 0m,
            decimal commissionAmount = 0m,
            decimal otherDeductions = 0m)
        {
            baseSalary = Math.Max(0m, baseSalary);
            overtimePay = Math.Max(0m, overtimePay);
            commissionAmount = Math.Max(0m, commissionAmount);
            otherDeductions = Math.Max(0m, otherDeductions);

            decimal grossPay = baseSalary + overtimePay + commissionAmount;

            // In Philippine compensation payroll, statutory mandatory contributions are based on basic salary
            decimal sss = CalculateSssContribution(baseSalary);
            decimal phic = CalculatePhilHealthContribution(baseSalary);
            decimal hdmf = CalculatePagIbigContribution(baseSalary);

            decimal totalStatutory = sss + phic + hdmf;
            decimal taxableIncome = Math.Max(0m, grossPay - totalStatutory);
            decimal withholdingTax = CalculateWithholdingTax(taxableIncome);

            decimal totalDeductions = totalStatutory + withholdingTax + otherDeductions;
            decimal netPay = Math.Max(0m, grossPay - totalDeductions);

            return new PayrollCalculationResult
            {
                BaseSalary = baseSalary,
                OvertimePay = overtimePay,
                CommissionAmount = commissionAmount,
                GrossPay = grossPay,
                SssDeduction = sss,
                PhilHealthDeduction = phic,
                PagIbigDeduction = hdmf,
                TaxableIncome = taxableIncome,
                WithholdingTax = withholdingTax,
                OtherDeductions = otherDeductions,
                TotalDeductions = totalDeductions,
                NetPay = netPay
            };
        }
    }
}
