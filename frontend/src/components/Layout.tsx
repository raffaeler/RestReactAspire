import { AppBar, Toolbar, Typography, Container, Box, Button } from '@mui/material';
import { Outlet, useNavigate } from 'react-router-dom';
import LocalHospitalIcon from '@mui/icons-material/LocalHospital';
import AdminPanelSettingsIcon from '@mui/icons-material/AdminPanelSettings';
import BarChartIcon from '@mui/icons-material/BarChart';

export default function Layout() {
  const navigate = useNavigate();

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="static">
        <Toolbar>
          <LocalHospitalIcon sx={{ mr: 2 }} />
          <Typography
            variant="h6"
            component="div"
            sx={{ flexGrow: 1, cursor: 'pointer' }}
            onClick={() => navigate('/')}
          >
            Day Hospital
          </Typography>
          <Button color="inherit" onClick={() => navigate('/patients')}>
            Patients
          </Button>
          <Button color="inherit" onClick={() => navigate('/exams')}>
            Exams
          </Button>
          <Button color="inherit" onClick={() => navigate('/doctors')}>
            Doctors
          </Button>
          <Button color="inherit" onClick={() => navigate('/statistics')} startIcon={<BarChartIcon />}>
            Statistics
          </Button>
          <Button color="inherit" onClick={() => navigate('/admin')} startIcon={<AdminPanelSettingsIcon />}>
            Admin
          </Button>
        </Toolbar>
      </AppBar>
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4, flex: 1 }}>
        <Outlet />
      </Container>
    </Box>
  );
}
