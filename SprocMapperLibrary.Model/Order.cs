﻿using System;
using System.Collections.Generic;

namespace Model
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderNumber { get; set; }
        public int CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public ICollection<OrderItem> OrderItemList { get; set; }
    }
}