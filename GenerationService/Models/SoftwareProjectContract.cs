namespace GenerationService.Models;


public record SoftwareProjectContract(
    Guid Id,               
    string ProjectName,    
    string ClientCompany,  
    string TechStack,      
    int TeamSize,          
    decimal Budget,       
    DateTime StartDate,  
    DateTime Deadline,   
    string ProjectManager 
);