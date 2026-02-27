import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { CssBaseline, ThemeProvider, createTheme } from '@mui/material';
import Layout from './components/Layout';
import PatientListPage from './pages/PatientListPage';
import PatientDetailPage from './pages/PatientDetailPage';
import PatientFormPage from './pages/PatientFormPage';
import ExamListPage from './pages/ExamListPage';
import ExamDetailPage from './pages/ExamDetailPage';
import ExamFormPage from './pages/ExamFormPage';
import DoctorListPage from './pages/DoctorListPage';
import DoctorDetailPage from './pages/DoctorDetailPage';
import DoctorFormPage from './pages/DoctorFormPage';
import DoctorExamListPage from './pages/DoctorExamListPage';
import AdminPage from './pages/AdminPage';

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#1976d2',
    },
  },
});

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Layout />}>
            <Route index element={<Navigate to="/patients" replace />} />
            <Route path="patients" element={<PatientListPage />} />
            <Route path="patients/new" element={<PatientFormPage />} />
            <Route path="patients/:id" element={<PatientDetailPage />} />
            <Route path="patients/:id/edit" element={<PatientFormPage />} />
            <Route path="patients/:patientId/exams" element={<ExamListPage />} />
            <Route path="patients/:patientId/exams/new" element={<ExamFormPage />} />
            <Route path="exams" element={<ExamListPage />} />
            <Route path="exams/new" element={<ExamFormPage />} />
            <Route path="exams/:id" element={<ExamDetailPage />} />
            <Route path="exams/:id/edit" element={<ExamFormPage />} />
            <Route path="doctors" element={<DoctorListPage />} />
            <Route path="doctors/new" element={<DoctorFormPage />} />
            <Route path="doctors/:id" element={<DoctorDetailPage />} />
            <Route path="doctors/:id/edit" element={<DoctorFormPage />} />
            <Route path="doctors/:doctorId/exams" element={<DoctorExamListPage />} />
            <Route path="admin" element={<AdminPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ThemeProvider>
  );
}

export default App
