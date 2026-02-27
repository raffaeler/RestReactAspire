import { useState, useEffect } from 'react';
import {
  Typography, Paper, Box, Button, TextField, Alert, CircularProgress,
} from '@mui/material';
import SaveIcon from '@mui/icons-material/Save';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { useParams, useNavigate } from 'react-router-dom';
import { apiClient } from '../api/apiClient';
import type { Doctor, CreateDoctorRequest, UpdateDoctorRequest } from '../types/doctor';
import type { Link } from '../types/hateoas';

export default function DoctorFormPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const isEdit = Boolean(id);

  const [formData, setFormData] = useState<CreateDoctorRequest>({
    firstName: '',
    lastName: '',
    specialty: '',
    email: '',
    phone: '',
  });
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [doctorLinks, setDoctorLinks] = useState<Link[]>([]);

  useEffect(() => {
    if (!isEdit) return;

    const fetchDoctor = async () => {
      try {
        const data = await apiClient.get<Doctor>(`/api/doctors/${id}`);
        setFormData({
          firstName: data.firstName,
          lastName: data.lastName,
          specialty: data.specialty,
          email: data.email,
          phone: data.phone,
        });
        setDoctorLinks(data.links);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load doctor');
      } finally {
        setLoading(false);
      }
    };
    fetchDoctor();
  }, [id, isEdit]);

  const handleChange = (field: keyof CreateDoctorRequest) => (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData(prev => ({ ...prev, [field]: e.target.value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);

    try {
      if (isEdit) {
        const updateLink = apiClient.findLink(doctorLinks, 'update');
        if (!updateLink) throw new Error('Update link not available');
        const updateRequest: UpdateDoctorRequest = {
          firstName: formData.firstName,
          lastName: formData.lastName,
          specialty: formData.specialty,
          email: formData.email,
          phone: formData.phone,
        };
        await apiClient.put<Doctor>(updateLink.href, updateRequest);
      } else {
        const link = await apiClient.getLink('doctors');
        await apiClient.post<Doctor>(link.href, formData);
      }
      navigate('/doctors');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save doctor');
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
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate('/doctors')}>
          Back to Doctors
        </Button>
      </Box>

      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          {isEdit ? 'Edit Doctor' : 'New Doctor'}
        </Typography>

        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

        <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <TextField label="First Name" value={formData.firstName} onChange={handleChange('firstName')} required />
          <TextField label="Last Name" value={formData.lastName} onChange={handleChange('lastName')} required />
          <TextField label="Specialty" value={formData.specialty} onChange={handleChange('specialty')} required
            placeholder="e.g., Cardiology, Neurology, Orthopedics" />
          <TextField label="Email" type="email" value={formData.email} onChange={handleChange('email')} required />
          <TextField label="Phone" value={formData.phone} onChange={handleChange('phone')} required />

          <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
            <Button type="submit" variant="contained" startIcon={<SaveIcon />} disabled={saving}>
              {saving ? 'Saving...' : 'Save'}
            </Button>
            <Button variant="outlined" onClick={() => navigate('/doctors')}>
              Cancel
            </Button>
          </Box>
        </Box>
      </Paper>
    </>
  );
}
