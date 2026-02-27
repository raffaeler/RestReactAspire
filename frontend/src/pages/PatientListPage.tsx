import { useState, useEffect, useCallback } from 'react';
import {
  Typography, Button, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, Paper, IconButton, Alert, CircularProgress, Box,
  TablePagination, TextField, InputAdornment, TableSortLabel,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import VisibilityIcon from '@mui/icons-material/Visibility';
import SearchIcon from '@mui/icons-material/Search';
import ClearIcon from '@mui/icons-material/Clear';
import { useNavigate } from 'react-router-dom';
import { apiClient } from '../api/apiClient';
import type { Patient, PatientList } from '../types/patient';

type SortDirection = 'asc' | 'desc';

export default function PatientListPage() {
  const navigate = useNavigate();
  const [patients, setPatients] = useState<Patient[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState('lastName');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');

  const fetchPatients = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const link = await apiClient.getLink('patients');
      const searchParam = search ? `&search=${encodeURIComponent(search)}` : '';
      const sortParams = `&sortBy=${encodeURIComponent(sortBy)}&sortDirection=${encodeURIComponent(sortDirection)}`;
      const data = await apiClient.get<PatientList>(`${link.href}?page=${page + 1}&pageSize=${rowsPerPage}${searchParam}${sortParams}`);
      setPatients(data.items);
      setTotalCount(data.pagination.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load patients');
    } finally {
      setLoading(false);
    }
  }, [page, rowsPerPage, search, sortBy, sortDirection]);

  useEffect(() => { fetchPatients(); }, [fetchPatients]);

  const handleDelete = async (patient: Patient) => {
    const deleteLink = apiClient.findLink(patient.links, 'delete');
    if (!deleteLink) return;

    if (!confirm(`Delete patient ${patient.firstName} ${patient.lastName}?`)) return;

    try {
      await apiClient.delete(deleteLink.href);
      await fetchPatients();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete patient');
    }
  };

  const handleChangePage = (_event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const handleSearch = () => {
    setPage(0);
    setSearch(searchInput);
  };

  const handleClearSearch = () => {
    setSearchInput('');
    setPage(0);
    setSearch('');
  };

  const handleSearchKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'Enter') {
      handleSearch();
    }
  };

  const handleSort = (column: string) => {
    if (sortBy === column) {
      setSortDirection(prev => prev === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(column);
      setSortDirection('asc');
    }
    setPage(0);
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
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">Patients</Typography>
        <Button variant="contained" startIcon={<AddIcon />} onClick={() => navigate('/patients/new')}>
          Add Patient
        </Button>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
        <TextField
          size="small"
          placeholder="Search by name, email, phone…"
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          onKeyDown={handleSearchKeyDown}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon />
                </InputAdornment>
              ),
            },
          }}
          sx={{ minWidth: 300 }}
        />
        <Button variant="contained" onClick={handleSearch}>Search</Button>
        {search && (
          <Button variant="outlined" startIcon={<ClearIcon />} onClick={handleClearSearch}>
            Clear
          </Button>
        )}
      </Box>

      {patients.length === 0 && totalCount === 0 ? (
        <Alert severity="info">No patients found. Add a new patient to get started.</Alert>
      ) : (
        <Paper>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell sortDirection={sortBy === 'lastName' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'lastName'}
                      direction={sortBy === 'lastName' ? sortDirection : 'asc'}
                      onClick={() => handleSort('lastName')}
                    >
                      Name
                    </TableSortLabel>
                  </TableCell>
                  <TableCell sortDirection={sortBy === 'dateOfBirth' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'dateOfBirth'}
                      direction={sortBy === 'dateOfBirth' ? sortDirection : 'asc'}
                      onClick={() => handleSort('dateOfBirth')}
                    >
                      Date of Birth
                    </TableSortLabel>
                  </TableCell>
                  <TableCell sortDirection={sortBy === 'email' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'email'}
                      direction={sortBy === 'email' ? sortDirection : 'asc'}
                      onClick={() => handleSort('email')}
                    >
                      Email
                    </TableSortLabel>
                  </TableCell>
                  <TableCell sortDirection={sortBy === 'phone' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'phone'}
                      direction={sortBy === 'phone' ? sortDirection : 'asc'}
                      onClick={() => handleSort('phone')}
                    >
                      Phone
                    </TableSortLabel>
                  </TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {patients.map((patient) => (
                  <TableRow key={patient.id} hover>
                    <TableCell>{patient.firstName} {patient.lastName}</TableCell>
                    <TableCell>{new Date(patient.dateOfBirth).toLocaleDateString()}</TableCell>
                    <TableCell>{patient.email}</TableCell>
                    <TableCell>{patient.phone}</TableCell>
                    <TableCell align="right">
                      <IconButton aria-label="view" onClick={() => navigate(`/patients/${patient.id}`)}>
                        <VisibilityIcon />
                      </IconButton>
                      <IconButton aria-label="edit" onClick={() => navigate(`/patients/${patient.id}/edit`)}>
                        <EditIcon />
                      </IconButton>
                      <IconButton aria-label="delete" onClick={() => handleDelete(patient)} color="error">
                        <DeleteIcon />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <TablePagination
            component="div"
            count={totalCount}
            page={page}
            onPageChange={handleChangePage}
            rowsPerPage={rowsPerPage}
            onRowsPerPageChange={handleChangeRowsPerPage}
            rowsPerPageOptions={[5, 10, 25]}
          />
        </Paper>
      )}
    </>
  );
}
