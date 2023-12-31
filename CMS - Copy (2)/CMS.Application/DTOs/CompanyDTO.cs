﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CMS.Application.DTOs
{
    public class CompanyDTO
    {//Updated class
        public int Id { get; set; }

        [Required(ErrorMessage = "The Name field is required.")]
        public string Name { get; set; }
        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }
        public string PersonName { get; set; }

            [RegularExpression(@"^(\+[0-9]{1,})?[0-9]+$", ErrorMessage = "Phone must start with '+' and then contain numeric digits.")]
        public string PhoneNumber { set; get; }
        [Required(ErrorMessage = "Please select a country")]
        public int CountryId { set; get; }
        public string CountryName { set; get; }
    }
}
