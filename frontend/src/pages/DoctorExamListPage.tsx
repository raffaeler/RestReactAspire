import { useState, useEffect, useCallback } from 'react';
import {
  Typography, Button, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, Paper, IconButton, Alert, CircularProgress, Box, Chip,
  TablePagination, TextField, InputAdornment, TableSortLabel,
} from '@mui/material';
import VisibilityIcon from '@mui/icons-material/Visibility';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import SearchIcon from '@mui/icons-material/Search';
import ClearIcon from '@mui/icons-material/Clear';
import { useNavigate, useParams } from 'react-router-dom';
import { apiClient } from '../api/apiClient';
import type { Exam, ExamList } from '../types/exam';
import type { Doctor } from '../types/doctor';

type SortDirection = 'asc' | 'desc';

export default function DoctorExamListPage() {
  const { doctorId } = useParams<{ doctorId: string }>();
  const navigate = useNavigate();
  const [exams, setExams] = useState<Exam[]>([]);
  const [doctor, setDoctor] = useState<Doctor | null>(null);
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
      const doctorData = await apiClient.get<Doctor>(`/api/doctors/${doctorId}`);
      setDoctor(doctorData);
      const examsLink = apiClient.findLink(doctorData.links, 'exams');
      if (!examsLink) throw new Error('Exams link not found on doctor');
      const searchParam = search ? `&search=${encodeURIComponent(search)}` : '';
      const sortParams = `&sortBy=${encodeURIComponent(sortBy)}&sortDirection=${encodeURIComponent(sortDirection)}`;
      const data = await apiClient.get<ExamList>(`${examsLink.href}?page=${page + 1}&pageSize=${rowsPerPage}${searchParam}${sortParams}`);
      setExams(data.items);
      setTotalCount(data.pagination.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load exams');
    } finally {
      setLoading(false);
    }
  }, [doctorId, page, rowsPerPage, search, sortBy, sortDirection]);

  useEffect(() => { fetchExams(); }, [fetchExams]);

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

  const title = doctor
    ? `Exams for Dr. ${doctor.firstName} ${doctor.lastName}`
    : 'Doctor Exams';

  return (
    <>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4">{title}</Typography>
        <Button startIcon={<ArrowBackIcon />} variant="outlined" onClick={() => navigate(`/doctors/${doctorId}`)}>
          Back to Doctor
        </Button>
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
        <Alert severity="info">No exams assigned to this doctor.</Alert>
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
