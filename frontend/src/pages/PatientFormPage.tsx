import { useState, useEffect } from 'react';
import { Typography, Paper, Box, Button, TextField, Alert, CircularProgress } from '@mui/material';
import SaveIcon from '@mui/icons-material/Save';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useParams, useNavigate } from 'react-router-dom';
import { apiClient } from '../api/apiClient';
import type { Patient, CreatePatientRequest } from '../types/patient';
import type { Link } from '../types/hateoas';

export default function PatientFormPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isEdit = Boolean(id);

  const [formData, setFormData] = useState<CreatePatientRequest>({
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    email: '',
    phone: '',
  });
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [patientLinks, setPatientLinks] = useState<Link[]>([]);

  useEffect(() => {
    if (!isEdit) return;

    const fetchPatient = async () => {
      try {
        const data = await apiClient.get<Patient>(`/api/patients/${id}`);
        setFormData({
          firstName: data.firstName,
          lastName: data.lastName,
          dateOfBirth: data.dateOfBirth,
          email: data.email,
          phone: data.phone,
        });
        setPatientLinks(data.links);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load patient');
      } finally {
        setLoading(false);
      }
    };
    fetchPatient();
  }, [id, isEdit]);

  const handleChange = (field: keyof CreatePatientRequest) => (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({ ...prev, [field]: e.target.value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    try {
      if (isEdit) {
        const updateLink = apiClient.findLink(patientLinks, 'update');
        if (!updateLink) throw new Error('Update link not available');
        await apiClient.put<Patient>(updateLink.href, formData);
      } else {
        const link = await apiClient.getLink('patients');
        await apiClient.post<Patient>(link.href, formData);
      }
      navigate('/patients');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save patient');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <>
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/patients')}>
          Back to Patients
        </Button>
      </Box>

      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          {isEdit ? 'Edit Patient' : 'New Patient'}
        </Typography>

        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

        <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <TextField label="First Name" value={formData.firstName} onChange={handleChange('firstName')} required />
          <TextField label="Last Name" value={formData.lastName} onChange={handleChange('lastName')} required />
          <TextField
            label="Date of Birth"
            type="date"
            value={formData.dateOfBirth}
            onChange={handleChange('dateOfBirth')}
            required
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField label="Email" type="email" value={formData.email} onChange={handleChange('email')} required />
          <TextField label="Phone" value={formData.phone} onChange={handleChange('phone')} required />

          <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
            <Button type="submit" variant="contained" startIcon={<SaveIcon />} disabled={saving}>
              {saving ? 'Saving...' : 'Save'}
            </Button>
            <Button variant="outlined" onClick={() => navigate('/patients')}>
              Cancel
            </Button>
          </Box>
        </Box>
      </Paper>
    </>
  );
}
