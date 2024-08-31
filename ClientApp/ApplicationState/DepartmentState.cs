namespace ClientApp.ApplicationState;

public class DepartmentState
{
    public Action? GeneralDepartmentAction { get; set; }
    public bool ShowGeneralDepartment { get; set; }

    public void GeneralDepartmentClicked()
    {
        ResetAllDepartments();
        ShowGeneralDepartment = true;
        GeneralDepartmentAction?.Invoke();
    }

    public void ResetAllDepartments()
    {
        ShowGeneralDepartment = false;
    }
}