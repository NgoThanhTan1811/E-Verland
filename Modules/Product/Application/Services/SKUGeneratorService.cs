using System.Collections.Generic;
using System.Linq;

namespace Modules.Product.Application.Services;

public class SKUGeneratorService
{
    public record SKUOption(string Key, List<string> Values);
    public record GeneratedSKU(string Code, Dictionary<string, string> OptionValues);

    /// <summary>
    /// Generates SKU combinations from variants/attributes.
    /// Example: variants = [{Key: "Color", Values: ["Red", "Blue"]}, {Key: "Size", Values: ["S", "M"]}]
    /// Result: [SKU(Code: "Red-S"), SKU(Code: "Red-M"), SKU(Code: "Blue-S"), SKU(Code: "Blue-M")]
    /// </summary>
    public List<GeneratedSKU> GenerateSKUs(List<SKUOption> variants)
    {
        if (variants.Count == 0)
            return [];

        // Get all combinations of option values
        var combinations = GetCombinations(variants.Select(v => v.Values).ToList());

        // Map combinations to SKU objects with option values
        var skus = new List<GeneratedSKU>();
        foreach (var combination in combinations)
        {
            var optionValues = new Dictionary<string, string>();
            for (int i = 0; i < variants.Count; i++)
            {
                optionValues[variants[i].Key] = combination[i];
            }

            var skuCode = string.Join("-", combination);
            skus.Add(new GeneratedSKU(skuCode, optionValues));
        }

        return skus;
    }

    /// <summary>
    /// Generates cartesian product combinations of arrays
    /// </summary>
    private List<List<string>> GetCombinations(List<List<string>> arrays)
    {
        if (arrays.Count == 0)
            return [[]];

        var result = new List<List<string>>();
        var firstArray = arrays[0];
        var restCombinations = GetCombinations(arrays.Skip(1).ToList());

        foreach (var item in firstArray)
        {
            foreach (var combination in restCombinations)
            {
                var newCombination = new List<string> { item };
                newCombination.AddRange(combination);
                result.Add(newCombination);
            }
        }

        return result;
    }
}
