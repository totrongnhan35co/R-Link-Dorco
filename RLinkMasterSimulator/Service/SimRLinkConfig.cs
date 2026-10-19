using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RLinkMasterSimulator.Models;

namespace RLinkMasterSimulator.Services;

public class SimRLinkConfig
{
    public RLinkSettings Settings { get; set; } = new();

    public List<ProductItem> Products { get; set; } = new()
    {
        new() { ProductId = "TH001", ProductName = "Sữa tươi tiệt trùng TH true MILK 180ml" },
        new() { ProductId = "TH002", ProductName = "Sữa tươi tiệt trùng TH true MILK 500ml" },
        new() { ProductId = "TH003", ProductName = "Sữa tươi tiệt trùng TH true MILK 1L" },
        new() { ProductId = "TH004", ProductName = "Sữa tươi tiệt trùng TH true MILK không đường 1L" },
        new() { ProductId = "TH005", ProductName = "Sữa tươi tiệt trùng TH true MILK ít đường 1L" },
        new() { ProductId = "TH006", ProductName = "Sữa tươi thanh trùng TH true MILK 500ml" },
        new() { ProductId = "TH007", ProductName = "Sữa tươi thanh trùng TH true MILK 1L" },
        new() { ProductId = "TH008", ProductName = "Sữa chua uống TH true YOGURT 180ml" },
        new() { ProductId = "TH009", ProductName = "Sữa chua ăn TH true YOGURT 100g" },
        new() { ProductId = "TH010", ProductName = "Bơ lạt TH true BUTTER 200g" },
    };
}