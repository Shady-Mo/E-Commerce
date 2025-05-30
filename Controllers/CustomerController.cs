﻿using DataBase.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data.Relation;
using Project.DTOs;
using Project.Enums;
using Project.Services.Implementations;
using Project.Services.Interfaces;
using Project.Tables;

namespace Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly AppDbContext context;
        private readonly IEmailService emailService;

        public CustomerController(AppDbContext context, IEmailService emailService)
        {
            this.context = context;
            this.emailService = emailService;
        }

        [HttpGet("ShowAllCustomers")]
        [Authorize(Roles = "Admin")]
        public IActionResult ShowAllCustomers()
        {
            var customers = context.Customers.
                Include(c => c.Orders)
                .ToList();
            if (customers == null || customers.Count == 0)
            {
                return NotFound(new { message = "No customers found." });
            }

            var result = customers.Select(p => new ShowCustomerDTO
            {
                CustomerId = p.Id,
                CustomerName = p.UserName,
                Status = p.Status.ToString(),
                ordercount = p.Orders.Count
            }).ToList();
            return Ok(result);
        }

        [HttpGet("ShowCustomerwithId")]
        [Authorize(Roles = "Admin")]
        public IActionResult ShowCustomerwithUserName(string Id)
        {
            var customers = context.Customers.
                 Include(c => c.Orders)
                .Where(e => e.Id == Id).ToList();
            if (customers == null || customers.Count == 0)
            {
                return NotFound(new { message = "No customers found with the given username." });
            }
            var result = customers.Select(p => new ShowCustomerDTO
            {
                CustomerId = p.Id,
                CustomerName = p.UserName,
                Status = p.Status.ToString(),
                ordercount = p.Orders.Count
            });

            return Ok(result);
        }

        [HttpPut("UpdateCustomerStatus")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateCustomerStatus(string customerId, string status)
        {
            var customer = context.Customers.FirstOrDefault(c => c.Id == customerId);
            if (customer == null)
            {
                return NotFound(new { message = "Customer not found" });
            }
            if (Enum.TryParse(status, out AccStatus customerStatus))
            {
                if (customerStatus != customer.Status)
                {
                    customer.Status = customerStatus;
                    context.SaveChanges();
                    return Ok(new { message = "Customer status updated successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Customer status is already the same" });
                }
            }
            return BadRequest(new { message = "Invalid status" });
            }



        [HttpPost("SaveCustomerLogs")]
        public async Task<IActionResult> SaveCustomerLogs(LogsDTO log)
        {

            if (!Enum.TryParse<EventStatus>(log.Eventtype, true, out var newStatus))
                return BadRequest(new { message = "Invalid status value." });

            var customer = await context.Customers.FindAsync(log.CustomerId);
            if (customer == null)
            {
                return NotFound(new { message = "Customer not found" });
            }
            var product = await context.Products.FindAsync(log.ProductId);
            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }
            var newLog = new History
            {
                customerId = log.CustomerId,
                productId = log.ProductId,
                event_type = newStatus
            };
            context.Histories.Add(newLog);
            await context.SaveChangesAsync();
            return Ok(new { message = "Log saved successfully" });

        }



        [HttpGet("ShowCustomerLogs")]        
        public IActionResult ShowCustomerLogs(string Id)
        {
            var customer = context.Customers
                .Include(c => c.histories)
                .ThenInclude(h => h.product)
                .FirstOrDefault(c => c.Id == Id);
            if (customer == null)
            {
                return NotFound(new { message = "Customer not found" });
            }
            var logs = customer.histories.Select(h => new LogsDTO
            {
                CustomerId = h.customerId,
                ProductId = h.productId,
                Eventtype = h.event_type.ToString()
            }).ToList();
            return Ok(logs);

        }


    }
}

