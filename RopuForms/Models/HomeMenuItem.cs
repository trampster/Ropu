﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RopuForms.Models
{
    public enum MenuItemType
    {
        Ptt,
        Browse,
        About
    }
    public class HomeMenuItem
    {
        public MenuItemType Id { get; set; }

        public string Title { get; set; }
    }
}
