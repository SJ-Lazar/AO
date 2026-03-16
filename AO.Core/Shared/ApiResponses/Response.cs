using System;
using System.Collections.Generic;
using System.Text;

namespace AO.Core.Shared.ApiResponses;

public class Response<T> 
{
    public bool Sucesss { get; set; }
    public T? Data { get; set; } 
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public Guid? RequestId { get; set; }

}
