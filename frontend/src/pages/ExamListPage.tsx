import { useState, useEffect, useCallback } from 'react';
import {
  Typography, Button, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, Paper, IconButton, Alert, CircularProgress, Box, Chip,
  TablePagination, TextField, InputAdornment, TableSortLabel,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import VisibilityIcon from '@mui/icons-material/Visibility';
import SearchIcon from '@mui/icons-material/Search';
import ClearIcon from '@mui/icons-material/Clear';
import { useNavigate, useParams } from 'react-router-dom';
import { apiClient } from '../api/apiClient';
import type { Exam, ExamList } from '../types/exam';
import type { Patient } from '../types/patient';

type SortDirection = 'asc' | 'desc';

export default function ExamListPage() {
  const { patientId } = useParams<{ patientId: string }>();
  const navigate = useNavigate();
  const [exams, setExams] = useState<Exam[]>([]);
  const [patient, setPatient] = useState<Patient | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [totalCount, setTotalCount] = useState(0);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [sortBy, setSortBy] = useState('scheduledDate');
  const [sortDirection, setSortDirection] = useState<SortDirection>('asc');

  const fetchExams = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const searchParam = search ? `&search=${encodeURIComponent(search)}` : '';
      const sortParams = `&sortBy=${encodeURIComponent(sortBy)}&sortDirection=${encodeURIComponent(sortDirection)}`;
      const paginationParams = `?page=${page + 1}&pageSize=${rowsPerPage}${searchParam}${sortParams}`;
      if (patientId) {
        const patientData = await apiClient.get<Patient>(`/api/patients/${patientId}`);
        setPatient(patientData);
        const examsLink = apiClient.findLink(patientData.links, 'exams');
        if (!examsLink) throw new Error('Exams link not found on patient');
        const data = await apiClient.get<ExamList>(`${examsLink.href}${paginationParams}`);
        setExams(data.items);
        setTotalCount(data.pagination.totalCount);
      } else {
        const link = await apiClient.getLink('exams');
        const data = await apiClient.get<ExamList>(`${link.href}${paginationParams}`);
        setExams(data.items);
        setTotalCount(data.pagination.totalCount);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load exams');
    } finally {
      setLoading(false);
    }
  }, [patientId, page, rowsPerPage, search, sortBy, sortDirection]);

  useEffect(() => { fetchExams(); }, [fetchExams]);

  const handleDelete = async (exam: Exam) => {
    const deleteLink = apiClient.findLink(exam.links, 'delete');
    if (!deleteLink) return;

    if (!confirm(`Delete ${exam.type} exam?`)) return;

    try {
      await apiClient.delete(deleteLink.href);
      await fetchExams();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete exam');
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

  const statusColor = (status: string): 'default' | 'primary' | 'success' | 'error' => {
    switch (status.toLowerCase()) {
      case 'scheduled': return 'primary';
      case 'completed': return 'success';
      case 'cancelled': return 'error';
      default: return 'default';
    }
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  const title = patient
    ? `Exams for ${patient.firstName} ${patient.lastName}`
    : 'All Exams';

  const newExamUrl = patientId ? `/patients/${patientId}/exams/new` : '/exams/new';

  return (
    <>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">{title}</Typography>
        <Box sx={{ display: 'flex', gap: 1 }}>
          {patient && (
            <Button variant="outlined" onClick={() => navigate(`/patients/${patientId}`)}>
              Back to Patient
            </Button>
          )}
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => navigate(newExamUrl)}>
            Add Exam
          </Button>
        </Box>
      </Box>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

      <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
        <TextField
          size="small"
          placeholder="Search by type, status, date, results…"
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

      {exams.length === 0 && totalCount === 0 ? (
        <Alert severity="info">No exams found. Add a new exam to get started.</Alert>
      ) : (
        <Paper>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell sortDirection={sortBy === 'type' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'type'}
                      direction={sortBy === 'type' ? sortDirection : 'asc'}
                      onClick={() => handleSort('type')}
                    >
                      Type
                    </TableSortLabel>
                  </TableCell>
                  <TableCell sortDirection={sortBy === 'scheduledDate' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'scheduledDate'}
                      direction={sortBy === 'scheduledDate' ? sortDirection : 'asc'}
                      onClick={() => handleSort('scheduledDate')}
                    >
                      Scheduled Date
                    </TableSortLabel>
                  </TableCell>
                  <TableCell sortDirection={sortBy === 'status' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'status'}
                      direction={sortBy === 'status' ? sortDirection : 'asc'}
                      onClick={() => handleSort('status')}
                    >
                      Status
                    </TableSortLabel>
                  </TableCell>
                  <TableCell sortDirection={sortBy === 'results' ? sortDirection : false}>
                    <TableSortLabel
                      active={sortBy === 'results'}
                      direction={sortBy === 'results' ? sortDirection : 'asc'}
                      onClick={() => handleSort('results')}
                    >
                      Results
                    </TableSortLabel>
                  </TableCell>
                  <TableCell align="right">Actions</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {exams.map((exam) => (
                  <TableRow key={exam.id} hover>
                    <TableCell>{exam.type}</TableCell>
                    <TableCell>{new Date(exam.scheduledDate).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <Chip label={exam.status} color={statusColor(exam.status)} size="small" />
                    </TableCell>
                    <TableCell>{exam.results ?? '—'}</TableCell>
                    <TableCell align="right">
                      <IconButton aria-label="view" onClick={() => navigate(`/exams/${exam.id}`)}>
                        <VisibilityIcon />
                      </IconButton>
                      <IconButton aria-label="edit" onClick={() => navigate(`/exams/${exam.id}/edit`)}>
                        <EditIcon />
                      </IconButton>
                      <IconButton aria-label="delete" onClick={() => handleDelete(exam)} color="error">
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
